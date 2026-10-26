using LoungeClubsApi.Entities.Base;
using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Entities;

/// <summary>A public complaint about an establishment (noise, unlicensed, underage, …).</summary>
public class Complaint : AuditableEntity
{
    public Guid? EstablishmentId { get; set; }
    public Establishment? Establishment { get; set; }
    /// <summary>Free-text venue name/area when the venue isn't in the registry.</summary>
    public string? VenueDescription { get; set; }
    public string Category { get; set; } = string.Empty; // Noise, UnderageEntry, UnlicensedOperation, Safety, Other
    public string Details { get; set; } = string.Empty;
    public string? ComplainantName { get; set; }
    public string? ComplainantPhone { get; set; }
    public ComplaintStatus Status { get; set; } = ComplaintStatus.New;
    public string? ResolutionNote { get; set; }
    public Guid? InspectionId { get; set; }
}
