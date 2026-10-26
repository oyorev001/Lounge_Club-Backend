using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Fines.IssueFine;

public record IssueFineResponse(
    Guid Id,
    string Reference,
    decimal Amount,
    FineStatus Status);
