using LoungeClubsApi.Entities.Base;
using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Entities;

public class Inspection : AuditableEntity
{
    public Guid EstablishmentId { get; set; }
    public Establishment Establishment { get; set; } = null!;
    public string OfficerId { get; set; } = string.Empty;
    public User Officer { get; set; } = null!;
    public InspectionType Type { get; set; }
    public DateTimeOffset? ScheduledFor { get; set; }
    public DateTimeOffset? ConductedAt { get; set; }
    public InspectionOutcome? Outcome { get; set; }
    /// <summary>Officer GPS at completion — proves the visit was on-site.</summary>
    public double? OfficerLatitude { get; set; }
    public double? OfficerLongitude { get; set; }
    public string? Notes { get; set; }
    public string? EvidenceUrls { get; set; } // JSON array of uploaded photo URLs
}
