namespace LoungeClubsApi.Features.Establishments.ReclassifyTier;

public record ReclassifyTierResponse(
    Guid Id,
    string TierCode,
    string TierName);
