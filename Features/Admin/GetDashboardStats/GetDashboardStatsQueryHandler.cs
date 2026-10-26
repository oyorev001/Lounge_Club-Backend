using LoungeClubsApi.Data;
using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Admin.GetDashboardStats;

public class GetDashboardStatsQueryHandler(AppDbContext db)
    : IQueryHandler<GetDashboardStatsQuery, DashboardStatsResponse>
{
    public async Task<DashboardStatsResponse> Handle(GetDashboardStatsQuery query, CancellationToken cancellationToken)
    {
        var establishments = new DashboardStatsResponse.EstablishmentStats(
            await db.Establishments.CountAsync(cancellationToken),
            await db.Establishments.CountAsync(e => e.Status == EstablishmentStatus.PendingReview, cancellationToken),
            await db.Establishments.CountAsync(e => e.Status == EstablishmentStatus.Approved, cancellationToken),
            await db.Establishments.CountAsync(e => e.Status == EstablishmentStatus.Suspended, cancellationToken),
            await db.Establishments.CountAsync(e => e.Status == EstablishmentStatus.NonCompliant, cancellationToken));

        var tiers = await db.LicenseTiers
            .AsNoTracking()
            .OrderBy(t => t.SortOrder)
            .Select(t => new DashboardStatsResponse.TierStat(
                t.Code,
                t.Name,
                // Only live venues count — rejected and deregistered records
                // still carry a tier value but aren't part of the licensed stock.
                t.Establishments.Count(e => e.Status != EstablishmentStatus.Rejected
                    && e.Status != EstablishmentStatus.Closed)))
            .ToListAsync(cancellationToken);

        var currentYear = DateTime.UtcNow.Year;
        var yearLevies = db.LevyAssessments.Where(l => l.Year == currentYear);

        var levies = new DashboardStatsResponse.LevyStats(
            currentYear,
            await yearLevies.CountAsync(cancellationToken),
            await yearLevies.CountAsync(l => l.Status == LevyStatus.Paid, cancellationToken),
            await yearLevies.CountAsync(l => l.Status == LevyStatus.Overdue, cancellationToken),
            await yearLevies.CountAsync(l => l.Status == LevyStatus.Waived, cancellationToken),
            await yearLevies.Where(l => l.Status != LevyStatus.Waived).SumAsync(l => l.Amount, cancellationToken),
            await yearLevies.Where(l => l.Status == LevyStatus.Paid).SumAsync(l => l.Amount, cancellationToken));

        var certificates = new DashboardStatsResponse.CertificateStats(
            await db.Certificates.CountAsync(c => c.Status == CertificateStatus.Active, cancellationToken),
            await db.Certificates.CountAsync(c => c.Status == CertificateStatus.Expired, cancellationToken),
            await db.Certificates.CountAsync(c => c.Status == CertificateStatus.Revoked, cancellationToken));

        var fines = new DashboardStatsResponse.FineStats(
            await db.Fines.CountAsync(f => f.Status == FineStatus.Unpaid, cancellationToken),
            await db.Fines.CountAsync(f => f.Status == FineStatus.Paid, cancellationToken),
            await db.Fines.Where(f => f.Status == FineStatus.Unpaid).SumAsync(f => f.Amount, cancellationToken),
            await db.Fines.Where(f => f.Status == FineStatus.Paid).SumAsync(f => f.Amount, cancellationToken));

        var now = DateTimeOffset.UtcNow;
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var inspections = new DashboardStatsResponse.InspectionStats(
            await db.Inspections.CountAsync(
                i => i.Outcome == null && i.ScheduledFor != null && i.ScheduledFor > now,
                cancellationToken),
            await db.Inspections.CountAsync(
                i => i.Outcome == null && i.ScheduledFor != null && i.ScheduledFor <= now,
                cancellationToken),
            await db.Inspections.CountAsync(
                i => i.ConductedAt != null && i.ConductedAt >= monthStart,
                cancellationToken));

        var openComplaints = await db.Complaints.CountAsync(
            c => c.Status != ComplaintStatus.Resolved && c.Status != ComplaintStatus.Dismissed,
            cancellationToken);

        var payments = new DashboardStatsResponse.PaymentStats(
            await db.Transactions.CountAsync(cancellationToken),
            await db.Transactions.Where(t => t.Status == PaymentStatus.Successful).SumAsync(t => t.Amount, cancellationToken));

        return new DashboardStatsResponse(
            establishments,
            tiers,
            levies,
            certificates,
            fines,
            inspections,
            openComplaints,
            payments);
    }
}
