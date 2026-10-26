using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Certificates.VerifyCertificate;

public record VerifyCertificateResponse(
    bool Found,
    string? CertificateNumber,
    string? EstablishmentName,
    EstablishmentType? EstablishmentType,
    string? Lga,
    string? TierName,
    int? LevyYear,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    CertificateStatus? Status,
    EstablishmentStatus? EstablishmentStatus,
    bool IsValid);
