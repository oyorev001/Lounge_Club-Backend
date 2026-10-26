using LoungeClubsApi.Features.Tiers.GetTiers;

namespace LoungeClubsApi.Features.Tiers.UpdateTier;

public record UpdateTierCommand(
    Guid Id,
    string Name,
    string? Description,
    decimal AnnualFee,
    int SortOrder,
    int MinScore,
    bool IsActive
) : ICommand<TierResponse>;
