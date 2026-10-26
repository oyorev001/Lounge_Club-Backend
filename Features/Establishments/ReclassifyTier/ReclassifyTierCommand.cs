namespace LoungeClubsApi.Features.Establishments.ReclassifyTier;

public record ReclassifyTierCommand(
    Guid EstablishmentId,
    Guid TierId,
    string Reason
) : ICommand<ReclassifyTierResponse>;
