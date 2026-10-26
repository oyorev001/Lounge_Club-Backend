namespace LoungeClubsApi.Configuration;

public class AppSettings
{
    public string AppUrl { get; set; } = string.Empty;
    public string PortalUrl { get; set; } = string.Empty;
    public int LevyDueDays { get; set; } = 30;
    /// <summary>Waivers above this amount require Admin.</summary>
    public decimal WaiverApprovalThreshold { get; set; } = 100_000m;
}
