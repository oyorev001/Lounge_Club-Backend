using LoungeClubsApi.Entities.Base;
using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Entities;

public class LevyAssessment : AuditableEntity
{
    public string Reference { get; set; } = string.Empty; // LEV-2026-000123
    public Guid EstablishmentId { get; set; }
    public Establishment Establishment { get; set; } = null!;
    public Guid TierId { get; set; }
    public LicenseTier Tier { get; set; } = null!;
    public int Year { get; set; }
    public decimal Amount { get; set; }
    public DateOnly DueDate { get; set; }
    public LevyStatus Status { get; set; } = LevyStatus.Pending;
    public DateTimeOffset? PaidAt { get; set; }
    public string? WaivedById { get; set; }
    public string? WaiverReason { get; set; }

    public Transaction? Transaction { get; set; }
    public Certificate? Certificate { get; set; }
}
