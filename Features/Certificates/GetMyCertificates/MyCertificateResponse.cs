using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Certificates.GetMyCertificates;

public record MyCertificateResponse(
    Guid Id,
    string CertificateNumber,
    string EstablishmentName,
    string? TierName,
    int LevyYear,
    decimal AmountPaid,
    DateOnly IssueDate,
    DateOnly ExpiryDate,
    CertificateStatus Status,
    bool IsValid);
