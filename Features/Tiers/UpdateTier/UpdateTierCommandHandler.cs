using LoungeClubsApi.Data;
using LoungeClubsApi.Features.Tiers.GetTiers;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Features.Tiers.UpdateTier;

public class UpdateTierCommandHandler(
    AppDbContext db,
    ICurrentUserService currentUser)
    : ICommandHandler<UpdateTierCommand, TierResponse>
{
    public async Task<TierResponse> Handle(UpdateTierCommand request, CancellationToken cancellationToken)
    {
        var tier = await db.LicenseTiers.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("License tier not found.");

        tier.Name = request.Name;
        tier.Description = request.Description;
        tier.AnnualFee = request.AnnualFee;
        tier.SortOrder = request.SortOrder;
        tier.MinScore = request.MinScore;
        tier.IsActive = request.IsActive;

        await db.SaveChangesAsync(cancellationToken);

        return new TierResponse(
            tier.Id,
            tier.Code,
            tier.Name,
            tier.Description,
            tier.AnnualFee,
            tier.SortOrder,
            tier.MinScore,
            tier.IsActive);
    }
}
