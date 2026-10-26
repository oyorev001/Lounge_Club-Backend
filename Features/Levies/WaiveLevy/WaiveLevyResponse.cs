using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Levies.WaiveLevy;

public record WaiveLevyResponse(
    Guid Id,
    LevyStatus Status);
