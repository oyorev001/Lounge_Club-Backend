using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Levies.GetMyLevies;

public record MyLevyResponse(
    Guid Id,
    string Reference,
    Guid EstablishmentId,
    string EstablishmentName,
    string TierCode,
    string TierName,
    int Year,
    decimal Amount,
    DateOnly DueDate,
    LevyStatus Status,
    DateTimeOffset? PaidAt,
    bool HasCertificate,
    Guid? CertificateId);
