using Microsoft.Extensions.DependencyInjection;
using LoungeClubsApi.Attributes;
using TickerQ.Utilities.Base;
using LoungeClubsApi.Data;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Features.Jobs;

/// <summary>Drains the SMS outbox every 2 minutes with exponential backoff.</summary>
[ScopedService]
public class SmsOutboxJob(IServiceScopeFactory scopeFactory, ILogger<SmsOutboxJob> logger)
{
    private static readonly TimeSpan[] Backoffs =
        [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30), TimeSpan.FromHours(2)];

    [TickerFunction("SmsOutbox", cronExpression: "0 */2 * * * *")]
    public async Task Run(TickerFunctionContext context, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sms = scope.ServiceProvider.GetRequiredService<ISmsService>();

        if (!sms.IsConfigured)
            return;

        var now = DateTimeOffset.UtcNow;
        var due = await db.SmsOutbox
            .Where(e => (e.Status == "Pending" && e.NextAttemptAt <= now && e.Attempts < 5)
                || (e.Status == "Sending" && e.NextAttemptAt <= now))
            .OrderBy(e => e.NextAttemptAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var msg in due)
        {
            // Same claim-lease as the email outbox — prevents double-sends and
            // lets a crashed row be reclaimed after the lease expires.
            var claimed = await db.SmsOutbox
                .Where(e => e.Id == msg.Id
                    && (e.Status == "Pending" || (e.Status == "Sending" && e.NextAttemptAt <= now)))
                .ExecuteUpdateAsync(u => u
                    .SetProperty(e => e.Status, "Sending")
                    .SetProperty(e => e.NextAttemptAt, now.AddMinutes(10)),
                    cancellationToken);
            if (claimed == 0)
                continue;
            msg.Status = "Sending";

            try
            {
                await sms.SendAsync(msg.To, msg.Message, cancellationToken);
                msg.Status = "Sent";
                msg.SentAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                msg.Attempts++;
                msg.LastError = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
                if (msg.Attempts >= 5)
                {
                    msg.Status = "Failed";
                    logger.LogError(ex, "SMS to {To} permanently failed", msg.To);
                }
                else
                {
                    msg.NextAttemptAt = DateTimeOffset.UtcNow + Backoffs[Math.Min(msg.Attempts - 1, Backoffs.Length - 1)];
                }
            }
        }

        if (due.Count > 0)
            await db.SaveChangesAsync(cancellationToken);
    }
}
