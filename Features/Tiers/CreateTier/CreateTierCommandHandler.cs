using LoungeClubsApi.Data;
using LoungeClubsApi.Entities;
using LoungeClubsApi.Features.Tiers.GetTiers;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Features.Tiers.CreateTier;

public class CreateTierCommandHandler(
    AppDbContext db,
    ICurrentUserService currentUser)
    : ICommandHandler<CreateTierCommand, TierResponse>
{
    public async Task<TierResponse> Handle(CreateTierCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();

        var codeExists = await db.LicenseTiers.AnyAsync(t => t.Code == code, cancellationToken);
        if (codeExists)
        {
            throw new InvalidOperationException($"A tier with code \"{code}\" already exists.");
        }

        var tier = new LicenseTier
        {
            Code = code,
            Name = request.Name,
            Description = request.Description,
            AnnualFee = request.AnnualFee,
            SortOrder = request.SortOrder,
            MinScore = request.MinScore,
            IsActive = true
        };

        db.LicenseTiers.Add(tier);
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
