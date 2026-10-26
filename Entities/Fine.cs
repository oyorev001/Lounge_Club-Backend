using LoungeClubsApi.Entities.Base;
using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Entities;

public class Fine : AuditableEntity
{
    public string Reference { get; set; } = string.Empty; // FIN-000123
    public Guid EstablishmentId { get; set; }
    public Establishment Establishment { get; set; } = null!;
    // Null for system-issued fines (enforcement ladder auto-penalties).
    public string? IssuedById { get; set; }
    public User? IssuedBy { get; set; }
    public Guid? InspectionId { get; set; }
    public Inspection? Inspection { get; set; }
    public string FineType { get; set; } = string.Empty; // UnlicensedOperation, ExpiredCertificate, FailedInspection, Other
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public FineStatus Status { get; set; } = FineStatus.Unpaid;
    public DateTimeOffset? PaidAt { get; set; }
    public string? WaivedById { get; set; }
    public string? WaiverReason { get; set; }

    public Transaction? Transaction { get; set; }
}
