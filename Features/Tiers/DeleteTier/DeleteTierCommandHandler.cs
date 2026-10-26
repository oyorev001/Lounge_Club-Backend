using LoungeClubsApi.Data;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Features.Tiers.DeleteTier;

public class DeleteTierCommandHandler(
    AppDbContext db,
    ICurrentUserService currentUser)
    : ICommandHandler<DeleteTierCommand, DeleteTierResponse>
{
    public async Task<DeleteTierResponse> Handle(DeleteTierCommand request, CancellationToken cancellationToken)
    {
        var tier = await db.LicenseTiers.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("License tier not found.");

        var inUse = await db.Establishments.AnyAsync(e => e.TierId == tier.Id, cancellationToken)
            || await db.LevyAssessments.AnyAsync(l => l.TierId == tier.Id, cancellationToken);
        if (inUse)
        {
            throw new InvalidOperationException("This tier is in use by establishments or levy assessments and cannot be deleted. Deactivate it instead.");
        }

        db.LicenseTiers.Remove(tier);
        await db.SaveChangesAsync(cancellationToken);

        return new DeleteTierResponse(tier.Id, $"Tier \"{tier.Code}\" was deleted.");
    }
}
