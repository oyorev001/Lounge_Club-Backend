using Microsoft.Extensions.DependencyInjection;
using LoungeClubsApi.Attributes;
using TickerQ.Utilities.Base;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities;
using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Jobs;

/// <summary>
/// Daily job (01:30 UTC) that (a) marks pending levy assessments past their
/// due date as overdue and notifies owners, and (b) sends certificate-expiry
/// reminders to owners exactly 30, 14 and 7 days before expiry.
/// The cron expression auto-seeds a CronTicker on startup.
/// </summary>
[ScopedService]
public class LevyReminderJob(IServiceScopeFactory scopeFactory, ILogger<LevyReminderJob> logger)
{
    [TickerFunction("LevyReminder", cronExpression: "0 30 1 * * *")]
    public async Task Run(
        TickerFunctionContext context,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Same-day dedupe: if the job is retried or run twice, notifications
        // already created today for the same recipient+link are skipped.
        var todayStart = new DateTimeOffset(today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var sentToday = (await db.Notifications
            .Where(n => n.CreatedAt >= todayStart
                && (n.Type == NotificationType.LevyDue || n.Type == NotificationType.CertificateExpiring))
            .Select(n => new { n.Type, n.UserId, n.Link })
            .ToListAsync(cancellationToken))
            .Select(n => $"{n.Type}|{n.UserId}|{n.Link}")
            .ToHashSet();

        // (a) Pending levies past due date -> Overdue + notify owner
        var overdue = await db.LevyAssessments
            .Include(l => l.Establishment)
                .ThenInclude(e => e.Owner)
            .Where(l => l.Status == LevyStatus.Pending && l.DueDate < today)
            .ToListAsync(cancellationToken);

        foreach (var levy in overdue)
        {
            levy.Status = LevyStatus.Overdue;

            // Include the levy id in the link so two overdue levies on the
            // same establishment each produce a notification (dedup key).
            var overdueLink = $"/dashboard/establishments/{levy.EstablishmentId}?levy={levy.Id}";
            if (!sentToday.Add($"{NotificationType.LevyDue}|{levy.Establishment.OwnerId}|{overdueLink}"))
                continue;

            QueueSms(db, levy.Establishment.Owner?.PhoneNumber,
                $"OYO LC: The annual levy {levy.Reference} for {levy.Establishment.Name} (N{levy.Amount:N0}) is now overdue. Pay on the portal to avoid penalties.");

            db.Notifications.Add(new Notification
            {
                UserId = levy.Establishment.OwnerId,
                Type = NotificationType.LevyDue,
                Title = "Levy payment overdue",
                Message = $"The annual levy {levy.Reference} for \"{levy.Establishment.Name}\" (₦{levy.Amount:N0}) was due on {levy.DueDate:dd MMM yyyy} and is now overdue.",
                Link = overdueLink
            });
        }

        // (b) Active certificates expiring in exactly 30/14/7 days -> notify owner.
        // Boundary dates pushed into SQL so we don't pull the whole table.
        var boundaryDates = new[] { today.AddDays(30), today.AddDays(14), today.AddDays(7) };
        var expiring = await db.Certificates
            .Include(c => c.Establishment)
                .ThenInclude(e => e.Owner)
            .Where(c => c.Status == CertificateStatus.Active && boundaryDates.Contains(c.ExpiryDate))
            .ToListAsync(cancellationToken);

        var reminded = 0;
        foreach (var certificate in expiring)
        {
            var daysUntilExpiry = certificate.ExpiryDate.DayNumber - today.DayNumber;
            if (daysUntilExpiry <= 0)
                continue;

            var reminderLink = $"/dashboard/establishments/{certificate.EstablishmentId}?cert={certificate.Id}";
            if (!sentToday.Add($"{NotificationType.CertificateExpiring}|{certificate.Establishment.OwnerId}|{reminderLink}"))
                continue;

            QueueSms(db, certificate.Establishment.Owner?.PhoneNumber,
                $"OYO LC: Certificate {certificate.CertificateNumber} for {certificate.Establishment.Name} expires {certificate.ExpiryDate:dd MMM yyyy} ({daysUntilExpiry} days). Renew on the portal.");

            db.Notifications.Add(new Notification
            {
                UserId = certificate.Establishment.OwnerId,
                Type = NotificationType.CertificateExpiring,
                Title = $"Certificate expires in {daysUntilExpiry} days",
                Message = $"The operating certificate {certificate.CertificateNumber} for \"{certificate.Establishment.Name}\" expires on {certificate.ExpiryDate:dd MMM yyyy}. Please renew to remain compliant.",
                Link = reminderLink
            });
            QueueEmail(db, certificate.Establishment.Owner?.Email,
                $"Certificate {certificate.CertificateNumber} expires in {daysUntilExpiry} days",
                $"The operating certificate <b>{certificate.CertificateNumber}</b> for " +
                $"<b>{certificate.Establishment.Name}</b> expires on " +
                $"<b>{certificate.ExpiryDate:dd MMM yyyy}</b> ({daysUntilExpiry} days). " +
                "Renew the annual levy on the portal to keep your licence valid.");
            reminded++;
        }

        // (c) Certificates hitting their expiry day → Expired + owner notified
        var expiredToday = await db.Certificates
            .Include(c => c.Establishment)
                .ThenInclude(e => e.Owner)
            .Where(c => c.Status == CertificateStatus.Active && c.ExpiryDate == today)
            .ToListAsync(cancellationToken);

        foreach (var certificate in expiredToday)
        {
            certificate.Status = CertificateStatus.Expired;
            var expiredLink = $"/dashboard/establishments/{certificate.EstablishmentId}?cert={certificate.Id}";

            QueueSms(db, certificate.Establishment.Owner?.PhoneNumber,
                $"OYO LC: Certificate {certificate.CertificateNumber} for {certificate.Establishment.Name} EXPIRED today. Renew on the portal to avoid enforcement.");

            db.Notifications.Add(new Notification
            {
                UserId = certificate.Establishment.OwnerId,
                Type = NotificationType.CertificateExpiring,
                Title = "Certificate expired today",
                Message = $"The operating certificate {certificate.CertificateNumber} for \"{certificate.Establishment.Name}\" expired today. Renew the annual levy to restore compliance.",
                Link = expiredLink
            });
            QueueEmail(db, certificate.Establishment.Owner?.Email,
                $"Certificate {certificate.CertificateNumber} has expired",
                $"The operating certificate <b>{certificate.CertificateNumber}</b> for " +
                $"<b>{certificate.Establishment.Name}</b> expired <b>today</b>. " +
                "Your venue is now unlicensed — renew the annual levy on the portal " +
                "immediately to avoid enforcement action.");
        }

        // Still-active certs already past expiry (missed runs / data repair)
        var staleExpired = await db.Certificates
            .Where(c => c.Status == CertificateStatus.Active && c.ExpiryDate < today)
            .ToListAsync(cancellationToken);
        foreach (var c in staleExpired)
            c.Status = CertificateStatus.Expired;

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "LevyReminder job completed — {OverdueCount} levies marked overdue, {ReminderCount} expiry reminders sent.",
            overdue.Count, reminded);
    }

    private static void QueueEmail(AppDbContext db, string? email, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(email))
            return;
        db.EmailOutbox.Add(new EmailOutbox
        {
            To = email,
            Subject = $"Oyo State — {subject}",
            HtmlBody = htmlBody
        });
    }

    private static void QueueSms(AppDbContext db, string? phone, string message)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return;
        var msisdn = Services.TermiiSmsService.NormalizeMsisdn(phone);
        if (msisdn.Length < 10)
            return;
        if (message.Length > 459)
            message = message[..456] + "...";
        db.SmsOutbox.Add(new SmsOutbox { To = msisdn, Message = message });
    }
}
