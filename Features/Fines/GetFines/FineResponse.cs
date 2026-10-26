using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Fines.GetFines;

public record FineResponse(
    Guid Id,
    string Reference,
    Guid EstablishmentId,
    string EstablishmentName,
    string? IssuedById,
    string? IssuedByName,
    string FineType,
    decimal Amount,
    string Reason,
    FineStatus Status,
    Guid? InspectionId,
    DateTimeOffset? PaidAt,
    DateTimeOffset CreatedAt);
