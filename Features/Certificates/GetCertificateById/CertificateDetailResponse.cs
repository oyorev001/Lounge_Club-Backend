using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Certificates.GetCertificateById;

public record CertificateDetailResponse(
    Guid Id,
    string CertificateNumber,
    Guid EstablishmentId,
    string EstablishmentName,
    string EstablishmentAddress,
    string EstablishmentLga,
    EstablishmentType EstablishmentType,
    string? TierName,
    int LevyYear,
    decimal AmountPaid,
    DateOnly IssueDate,
    DateOnly ExpiryDate,
    CertificateStatus Status,
    bool IsValid,
    string? RevokedReason,
    DateTimeOffset? RevokedAt);
