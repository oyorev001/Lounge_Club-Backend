namespace LoungeClubsApi.Features.Admin.GetDashboardStats;

public record DashboardStatsResponse(
    DashboardStatsResponse.EstablishmentStats Establishments,
    List<DashboardStatsResponse.TierStat> Tiers,
    DashboardStatsResponse.LevyStats Levies,
    DashboardStatsResponse.CertificateStats Certificates,
    DashboardStatsResponse.FineStats Fines,
    DashboardStatsResponse.InspectionStats Inspections,
    int OpenComplaints,
    DashboardStatsResponse.PaymentStats Payments)
{
    public record EstablishmentStats(
        int Total,
        int PendingReview,
        int Approved,
        int Suspended,
        int NonCompliant);

    public record TierStat(
        string Code,
        string Name,
        int Count);

    public record LevyStats(
        int CurrentYear,
        int Issued,
        int Paid,
        int Overdue,
        int Waived,
        decimal ExpectedRevenue,
        decimal CollectedRevenue);

    public record CertificateStats(
        int Active,
        int Expired,
        int Revoked);

    public record FineStats(
        int Unpaid,
        int Paid,
        decimal OutstandingAmount,
        decimal CollectedAmount);

    public record InspectionStats(
        int ScheduledUpcoming,
        int AwaitingReport,
        int CompletedThisMonth);

    public record PaymentStats(
        int TotalTransactions,
        decimal SuccessfulAmount);
}
