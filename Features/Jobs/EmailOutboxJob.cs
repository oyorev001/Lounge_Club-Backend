using Microsoft.Extensions.DependencyInjection;
using LoungeClubsApi.Attributes;
using TickerQ.Utilities.Base;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Features.Jobs;

/// <summary>
/// Drains the EmailOutbox every minute: sends pending rows, retries failures
/// with exponential backoff (1m → 5m → 30m → 2h), and gives up after 5 attempts.
/// </summary>
[ScopedService]
public class EmailOutboxJob(IServiceScopeFactory scopeFactory, ILogger<EmailOutboxJob> logger)
{
    private static readonly TimeSpan[] Backoffs =
        [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30), TimeSpan.FromHours(2)];

    [TickerFunction("EmailOutbox", cronExpression: "0 * * * * *")]
    public async Task Run(TickerFunctionContext context, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var now = DateTimeOffset.UtcNow;
        var due = await db.EmailOutbox
            // Pending rows due now, plus Sending rows whose 10-minute lease
            // expired — a crashed instance's rows auto-reclaim.
            .Where(e => (e.Status == EmailOutboxStatus.Pending && e.NextAttemptAt <= now && e.Attempts < 5)
                || (e.Status == EmailOutboxStatus.Sending && e.NextAttemptAt <= now))
            .OrderBy(e => e.NextAttemptAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        var sent = 0;
        foreach (var email in due)
        {
            // Atomic claim — two concurrent job instances can't send the same
            // email twice, and the lease lets a crashed row be re-taken.
            var claimed = await db.EmailOutbox
                .Where(e => e.Id == email.Id
                    && (e.Status == EmailOutboxStatus.Pending
                        || (e.Status == EmailOutboxStatus.Sending && e.NextAttemptAt <= now)))
                .ExecuteUpdateAsync(u => u
                    .SetProperty(e => e.Status, EmailOutboxStatus.Sending)
                    .SetProperty(e => e.NextAttemptAt, now.AddMinutes(10)),
                    cancellationToken);
            if (claimed == 0)
                continue;
            email.Status = EmailOutboxStatus.Sending;

            try
            {
                List<EmailAttachment>? attachments = string.IsNullOrWhiteSpace(email.Attachments)
                    ? null
                    : System.Text.Json.JsonSerializer.Deserialize<List<EmailAttachment>>(email.Attachments);
                await emailService.SendEmailAsync(email.To, email.Subject, email.HtmlBody, attachments);
                email.Status = EmailOutboxStatus.Sent;
                email.SentAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
                sent++;
            }
            catch (Exception ex)
            {
                email.Attempts++;
                // Column is varchar(2000) — an untruncated exception would fail
                // the batch save and poison the whole queue.
                email.LastError = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
                if (email.Attempts >= 5)
                {
                    email.Status = EmailOutboxStatus.Failed;
                    logger.LogError(ex, "Email to {To} permanently failed after {Attempts} attempts", email.To, email.Attempts);
                }
                else
                {
                    email.NextAttemptAt = DateTimeOffset.UtcNow + Backoffs[Math.Min(email.Attempts - 1, Backoffs.Length - 1)];
                }
            }
        }

        if (due.Count > 0)
            await db.SaveChangesAsync(cancellationToken);

        if (sent > 0)
            logger.LogInformation("EmailOutbox job sent {Count} emails ({Remaining} processed)", sent, due.Count);
    }
}
