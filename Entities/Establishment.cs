using LoungeClubsApi.Entities.Base;
using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Entities;

public class Establishment : AuditableEntity
{
    public string RegistrationNumber { get; set; } = string.Empty; // EST-000001
    public string Name { get; set; } = string.Empty;
    public EstablishmentType Type { get; set; } // primary (highest) classification
    public List<EstablishmentType> AdditionalTypes { get; set; } = []; // other operations declared
    public string? Description { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Lga { get; set; } = string.Empty;
    public string? Landmark { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string ContactPhone { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? CacNumber { get; set; }
    public string OwnerNin { get; set; } = string.Empty; // owner's 11-digit NIN

    // Declared stats (used for tier suggestion)
    public int Capacity { get; set; }
    public int StaffCount { get; set; }
    public int? FloorAreaSqm { get; set; }
    public bool HasLiveBand { get; set; }
    public bool HasPoolTable { get; set; }
    public bool HasRestaurant { get; set; }
    public bool HasOutdoorSpace { get; set; }
    public bool HasDj { get; set; }
    public bool HasVipArea { get; set; }
    public bool HasShisha { get; set; }
    public bool HasRooftop { get; set; }
    public bool HasLodging { get; set; }
    public bool HasParking { get; set; }
    public int? ParkingSpaces { get; set; }
    public string? OperatingHours { get; set; } // e.g. "6pm - 4am"

    public EstablishmentStatus Status { get; set; } = EstablishmentStatus.PendingReview;

    /// <summary>Why the venue was flagged — "UnpaidLevy" | "CriticalInspection" |
    /// "CertRevoked". A levy payment only self-clears UnpaidLevy flags.</summary>
    public string? NonCompliantReason { get; set; }
    public string? RejectionReason { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public string? ApprovedById { get; set; }
    public int? SuggestedTierScore { get; set; } // score computed at registration

    public string OwnerId { get; set; } = string.Empty;
    public User Owner { get; set; } = null!;

    public Guid? TierId { get; set; }
    public LicenseTier? Tier { get; set; }

    public ICollection<EstablishmentDocument> Documents { get; set; } = [];
    public ICollection<LevyAssessment> LevyAssessments { get; set; } = [];
    public ICollection<Certificate> Certificates { get; set; } = [];
    public ICollection<Inspection> Inspections { get; set; } = [];
    public ICollection<Fine> Fines { get; set; } = [];
}
