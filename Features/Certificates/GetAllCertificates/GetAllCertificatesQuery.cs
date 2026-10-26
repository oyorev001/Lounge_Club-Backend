using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Models.Shared;

namespace LoungeClubsApi.Features.Certificates.GetAllCertificates;

public class GetAllCertificatesRequest : PaginationRequest
{
    [FromQuery(Name = "search")]
    public string? Search { get; set; }

    [FromQuery(Name = "status")]
    public CertificateStatus? Status { get; set; }

    [FromQuery(Name = "year")]
    public int? Year { get; set; }

    [FromQuery(Name = "establishment_id")]
    public Guid? EstablishmentId { get; set; }

    [FromQuery(Name = "tier")]
    public string? Tier { get; set; }

    /// <summary>Active certs expiring within this many days.</summary>
    [FromQuery(Name = "expiring_in_days")]
    public int? ExpiringInDays { get; set; }

    /// <summary>Certs issued within the last N days ("newly renewed").</summary>
    [FromQuery(Name = "issued_within_days")]
    public int? IssuedWithinDays { get; set; }
}

public record GetAllCertificatesQuery(GetAllCertificatesRequest Request)
    : IQuery<PaginatedResponse<CertificateListItem>>;

public record CertificateListItem(
    Guid Id,
    string CertificateNumber,
    string EstablishmentName,
    string EstablishmentType,
    string Lga,
    string? TierName,
    int LevyYear,
    decimal AmountPaid,
    DateOnly IssueDate,
    DateOnly ExpiryDate,
    CertificateStatus Status,
    bool IsValid);
