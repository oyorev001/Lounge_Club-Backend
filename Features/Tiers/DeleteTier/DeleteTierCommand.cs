namespace LoungeClubsApi.Features.Tiers.DeleteTier;

public record DeleteTierCommand([Required] Guid Id) : ICommand<DeleteTierResponse>;
