using Microsoft.Extensions.DependencyInjection;
using LoungeClubsApi.Attributes;
using TickerQ.Utilities.Base;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Interfaces;
using LoungeClubsApi.Services;
using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Jobs;

/// <summary>
/// Daily compliance jobs:
///  (a) RenewalJob — auto-issues next-year levies ~60 days before certificates
///      expire so owners can renew before losing validity.
///  (b) EnforcementLadder — overdue >30d gets an automatic late-payment fine,
///      overdue >90d flags the establishment non-compliant and suspends certs.
///  (c) SlaEscalation — applications pending review beyond the SLA get
///      escalated to the Director General.
/// </summary>
[ScopedService]
public class ComplianceJobs(IServiceScopeFactory scopeFactory, ILogger<ComplianceJobs> logger)
{
    private const int RenewalLeadDays = 60;
    private const int LateFineAfterDays = 30;
    private const int NonCompliantAfterDays = 90;
    private const int ReviewSlaDays = 7;
    private const decimal LateFineAmount = 25_000m;

    [TickerFunction("RenewalJob", cronExpression: "0 0 2 * * *")]
    public async Task RunRenewals(TickerFunctionContext context, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var cutoff = today.AddDays(RenewalLeadDays);

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Active certs expiring within the renewal window
        var expiring = await db.Certificates
            .Include(c => c.Establishment)
                .ThenInclude(e => e.Tier)
            .Include(c => c.Establishment)
                .ThenInclude(e => e.Owner)
            .Where(c => c.Status == CertificateStatus.Active && c.ExpiryDate <= cutoff && c.ExpiryDate >= today)
            .ToListAsync(cancellationToken);

        var levies = new List<Entities.LevyAssessment>();
        var created = 0;
        foreach (var cert in expiring)
        {
            var est = cert.Establishment;
            if (est.Tier is null)
                continue;

            var nextYear = cert.LevyYear + 1;
            var alreadyAssessed = await db.LevyAssessments
                .AnyAsync(l => l.EstablishmentId == est.Id && l.Year == nextYear, cancellationToken);
            if (alreadyAssessed)
                continue;

            var countForYear = await db.LevyAssessments.CountAsync(l => l.Year == nextYear, cancellationToken) + 1;
            var levy = new LevyAssessment
            {
                Reference = Helpers.ReferenceGenerator.Yearly("LEV", nextYear, countForYear),
                EstablishmentId = est.Id,
                TierId = est.Tier.Id,
                Year = nextYear,
                Amount = est.Tier.AnnualFee,
                DueDate = cert.ExpiryDate, // pay before the current certificate lapses
                Status = LevyStatus.Pending
            };
            db.LevyAssessments.Add(levy);
            levies.Add(levy);
            created++;

            db.Notifications.Add(new Notification
            {
                UserId = est.OwnerId,
                Type = NotificationType.LevyDue,
                Title = $"Renew your compliance certificate — due {cert.ExpiryDate:dd MMM yyyy}",
                Message = $"Your certificate for \"{est.Name}\" expires {cert.ExpiryDate:dd MMM yyyy}. A renewal levy of ₦{levy.Amount:N2} ({levy.Reference}) is ready — pay now to keep your certificate valid without interruption.",
                Link = $"/dashboard/levies?focus={levy.Id}"
            });
            db.EmailOutbox.Add(new EmailOutbox
            {
                To = est.Owner.Email!,
                Subject = $"Certificate renewal due — {est.Name}",
                HtmlBody = $"Your certificate for <b>{est.Name}</b> expires <b>{cert.ExpiryDate:dd MMM yyyy}</b>. A renewal levy of <b>₦{levy.Amount:N2}</b> ({levy.Reference}) is ready on the portal — pay now to keep your certificate valid."
            });
            var msisdn = TermiiSmsService.NormalizeMsisdn(est.Owner.PhoneNumber ?? "");
            if (msisdn.Length >= 10)
            {
                db.SmsOutbox.Add(new SmsOutbox
                {
                    To = msisdn,
                    Message = $"OYO LC: Your certificate for {est.Name} expires {cert.ExpiryDate:dd MMM yyyy}. Renewal levy {levy.Reference} (N{levy.Amount:N0}) is ready — pay on the portal."
                });
            }
        }

        if (created > 0)
        {
            await db.SaveChangesWithUniqueRetryAsync(async () =>
            {
                // Drop levies a concurrent run/approval already assessed (the
                // unique EstId+Year index), then re-number the survivors.
                var nowAssessed = (await db.LevyAssessments
                    .Where(l => levies.Select(x => x.EstablishmentId).Contains(l.EstablishmentId)
                        && levies.Select(x => x.Year).Contains(l.Year))
                    .Select(l => new { l.EstablishmentId, l.Year })
                    .ToListAsync(cancellationToken))
                    .ToHashSet();
                foreach (var colliding in levies
                    .Where(l => nowAssessed.Contains(new { l.EstablishmentId, l.Year }))
                    .ToList())
                {
                    db.Entry(colliding).State = EntityState.Detached;
                    levies.Remove(colliding);
                }
                foreach (var group in levies.GroupBy(l => l.Year))
                {
                    var next = await db.LevyAssessments
                        .CountAsync(l => l.Year == group.Key, cancellationToken) + 1;
                    foreach (var levy in group)
                        levy.Reference = Helpers.ReferenceGenerator.Yearly("LEV", group.Key, next++);
                }
            }, cancellationToken);
            logger.LogInformation("RenewalJob issued {Count} renewal assessments", created);
        }
    }

    [TickerFunction("EnforcementLadder", cronExpression: "0 15 2 * * *")]
    public async Task RunEnforcement(TickerFunctionContext context, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var overdue = await db.LevyAssessments
            .Include(l => l.Establishment)
                .ThenInclude(e => e.Owner)
            .Where(l => l.Status == LevyStatus.Overdue)
            .ToListAsync(cancellationToken);

        var fines = 0;
        var flagged = 0;
        // Running sequence — Added fines aren't counted by the query, so
        // re-counting inside the loop would hand every fine the same reference.
        var fineSeq = await db.Fines.CountAsync(f => f.FineType == "LatePayment", cancellationToken) + 1;

        foreach (var levy in overdue)
        {
            var daysOverdue = (today.DayNumber - levy.DueDate.DayNumber);
            var est = levy.Establishment;

            // Late fine dedup is scoped to THIS levy — two overdue levies on
            // the same venue each earn their own fine, and paying the fine
            // (without paying the levy) doesn't re-arm another one.
            var levyFined = await db.Fines.AnyAsync(
                f => f.EstablishmentId == est.Id
                    && f.FineType == "LatePayment"
                    && f.Reason.Contains(levy.Reference),
                cancellationToken);

            if (!levyFined && daysOverdue >= LateFineAfterDays)
            {
                db.Fines.Add(new Fine
                {
                    Reference = Helpers.ReferenceGenerator.Yearly("FINE", today.Year, fineSeq++),
                    EstablishmentId = est.Id,
                    FineType = "LatePayment",
                    Amount = LateFineAmount,
                    Reason = $"Automatic penalty — levy {levy.Reference} unpaid {daysOverdue} days past due date.",
                    Status = FineStatus.Unpaid
                });
                fines++;

                db.Notifications.Add(new Notification
                {
                    UserId = est.OwnerId,
                    Type = NotificationType.FineIssued,
                    Title = $"Late payment penalty — {est.Name}",
                    Message = $"A ₦{LateFineAmount:N2} fine has been issued — levy {levy.Reference} is {daysOverdue} days overdue. Pay both to restore good standing.",
                    Link = $"/dashboard/fines"
                });
            }

            if (daysOverdue >= NonCompliantAfterDays)
            {
                if (est.Status == EstablishmentStatus.Approved)
                {
                    est.Status = EstablishmentStatus.NonCompliant;
                    est.NonCompliantReason = "UnpaidLevy";
                    flagged++;

                    var certs = await db.Certificates
                        .Where(c => c.EstablishmentId == est.Id && c.Status == CertificateStatus.Active)
                        .ToListAsync(cancellationToken);
                    foreach (var cert in certs)
                    {
                        cert.Status = CertificateStatus.Revoked;
                        cert.RevokedReason = $"Levy {levy.Reference} unpaid for {daysOverdue} days.";
                        cert.RevokedAt = now;
                    }

                    db.Notifications.Add(new Notification
                    {
                        UserId = est.OwnerId,
                        Type = NotificationType.ApplicationStatus,
                        Title = "Establishment flagged non-compliant",
                        Message = $"\"{est.Name}\" has been flagged non-compliant — levy {levy.Reference} is {daysOverdue} days overdue and active certificates were suspended. Pay to restore compliance.",
                        Link = $"/dashboard/levies?focus={levy.Id}"
                    });
                }
            }
        }

        if (fines > 0 || flagged > 0)
        {
            await db.SaveChangesWithUniqueRetryAsync(async () =>
            {
                var seq = await db.Fines.CountAsync(f => f.FineType == "LatePayment", cancellationToken) + 1;
                foreach (var fine in db.ChangeTracker.Entries<Fine>()
                    .Where(e => e.State == EntityState.Added).Select(e => e.Entity))
                    fine.Reference = Helpers.ReferenceGenerator.Yearly("FINE", today.Year, seq++);
            }, cancellationToken);
            logger.LogInformation("EnforcementLadder: {Fines} fines issued, {Flagged} establishments flagged", fines, flagged);
        }
    }

    [TickerFunction("SlaEscalation", cronExpression: "0 30 2 * * *")]
    public async Task RunSlaEscalation(TickerFunctionContext context, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var cutoff = now.AddDays(-ReviewSlaDays);

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var stale = await db.Establishments
            .Where(e => e.Status == EstablishmentStatus.PendingReview && e.CreatedAt < cutoff)
            .ToListAsync(cancellationToken);

        if (stale.Count == 0)
            return;

        // Notify Admins once per stale batch — dedupe on today's link.
        var dgUsers = await db.UserRoles
            .Join(db.Roles.Where(r => r.Name == AppRole.Admin.ToString()),
                ur => ur.RoleId, r => r.Id, (ur, _) => ur.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var todayStart = new DateTimeOffset(now.Date, TimeSpan.Zero);
        foreach (var dg in dgUsers)
        {
            var alreadyNotified = await db.Notifications
                .AnyAsync(n => n.UserId == dg
                    && n.Type == NotificationType.General
                    && n.CreatedAt >= todayStart
                    && n.Link == "/admin/establishments?status=PendingReview", cancellationToken);
            if (alreadyNotified)
                continue;

            db.Notifications.Add(new Notification
            {
                UserId = dg,
                Type = NotificationType.General,
                Title = $"{stale.Count} application(s) pending review beyond {ReviewSlaDays}-day SLA",
                Message = $"{stale.Count} establishment application(s) have waited over {ReviewSlaDays} days for review: {string.Join(", ", stale.Take(5).Select(e => e.Name))}{(stale.Count > 5 ? "…" : "")}",
                Link = "/admin/establishments?status=PendingReview"
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("SlaEscalation: escalated {Count} stale applications to {DGs} DG user(s)", stale.Count, dgUsers.Count);
    }
}
