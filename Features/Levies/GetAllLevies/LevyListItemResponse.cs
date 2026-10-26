using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Levies.GetAllLevies;

public record LevyListItemResponse(
    Guid Id,
    string Reference,
    Guid EstablishmentId,
    string EstablishmentName,
    string Lga,
    string TierCode,
    string TierName,
    int Year,
    decimal Amount,
    DateOnly DueDate,
    LevyStatus Status,
    DateTimeOffset? PaidAt,
    bool HasCertificate,
    Guid? CertificateId);
