using LoungeClubsApi.Entities.Base;
using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Entities;

public class Certificate : AuditableEntity
{
    public string CertificateNumber { get; set; } = string.Empty; // OYS-LC-2026-000123
    public string QrToken { get; set; } = string.Empty; // embedded in QR -> /verify/{token}
    public Guid EstablishmentId { get; set; }
    public Establishment Establishment { get; set; } = null!;
    public Guid LevyAssessmentId { get; set; }
    public LevyAssessment LevyAssessment { get; set; } = null!;
    public int LevyYear { get; set; }
    public decimal AmountPaid { get; set; }
    public DateOnly IssueDate { get; set; }
    public DateOnly ExpiryDate { get; set; }
    public CertificateStatus Status { get; set; } = CertificateStatus.Active;
    public string? RevokedReason { get; set; }
    public string? RevokedById { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
