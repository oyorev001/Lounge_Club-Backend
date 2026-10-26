namespace LoungeClubsApi.Features.Tiers.GetTiers;

public record TierResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    decimal AnnualFee,
    int SortOrder,
    int MinScore,
    bool IsActive);
