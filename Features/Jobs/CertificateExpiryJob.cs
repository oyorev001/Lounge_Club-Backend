using Microsoft.Extensions.DependencyInjection;
using LoungeClubsApi.Attributes;
using TickerQ.Utilities.Base;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities;
using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Jobs;

/// <summary>
/// Daily job (01:00 UTC) that expires certificates past their expiry date,
/// flags the owning establishments as non-compliant and notifies the owners.
/// The cron expression auto-seeds a CronTicker on startup.
/// </summary>
[ScopedService]
public class CertificateExpiryJob(IServiceScopeFactory scopeFactory, ILogger<CertificateExpiryJob> logger)
{
    [TickerFunction("CertificateExpiry", cronExpression: "0 0 1 * * *")]
    public async Task Run(
        TickerFunctionContext context,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var expired = await db.Certificates
            .Include(c => c.Establishment)
            .Where(c => c.Status == CertificateStatus.Active && c.ExpiryDate < today)
            .ToListAsync(cancellationToken);

        if (expired.Count == 0)
        {
            logger.LogInformation("CertificateExpiry job ran — no certificates to expire.");
            return;
        }

        foreach (var certificate in expired)
        {
            certificate.Status = CertificateStatus.Expired;

            if (certificate.Establishment.Status == EstablishmentStatus.Approved)
                certificate.Establishment.Status = EstablishmentStatus.NonCompliant;

            db.Notifications.Add(new Notification
            {
                UserId = certificate.Establishment.OwnerId,
                Type = NotificationType.CertificateExpiring,
                Title = "Certificate expired",
                Message = $"The operating certificate {certificate.CertificateNumber} for \"{certificate.Establishment.Name}\" expired on {certificate.ExpiryDate:dd MMM yyyy}. The establishment has been flagged non-compliant.",
                Link = $"/dashboard/establishments/{certificate.EstablishmentId}"
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("CertificateExpiry job expired {Count} certificates.", expired.Count);
    }
}
