using LoungeClubsApi.Features.Tiers.GetTiers;

namespace LoungeClubsApi.Features.Tiers.CreateTier;

public record CreateTierCommand(
    string Code,
    string Name,
    string? Description,
    decimal AnnualFee,
    int SortOrder,
    int MinScore
) : ICommand<TierResponse>;
