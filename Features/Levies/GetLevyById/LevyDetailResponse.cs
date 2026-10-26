using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Levies.GetLevyById;

public record LevyDetailResponse(
    Guid Id,
    string Reference,
    Guid EstablishmentId,
    string EstablishmentName,
    string EstablishmentRegistrationNumber,
    string Lga,
    string OwnerId,
    Guid TierId,
    string TierCode,
    string TierName,
    int Year,
    decimal Amount,
    DateOnly DueDate,
    LevyStatus Status,
    DateTimeOffset? PaidAt,
    string? WaivedById,
    string? WaiverReason,
    Guid? TransactionId,
    string? TransactionReference,
    PaymentStatus? TransactionStatus,
    Guid? CertificateId,
    string? CertificateNumber);
