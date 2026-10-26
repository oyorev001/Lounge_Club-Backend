using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Fines.GetMyFines;

public record MyFineResponse(
    Guid Id,
    string Reference,
    Guid EstablishmentId,
    string EstablishmentName,
    string FineType,
    decimal Amount,
    string Reason,
    FineStatus Status,
    DateTimeOffset? PaidAt,
    DateTimeOffset CreatedAt);
