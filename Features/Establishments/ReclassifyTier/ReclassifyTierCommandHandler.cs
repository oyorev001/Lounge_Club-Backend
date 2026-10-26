using LoungeClubsApi.Data;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Interfaces;
using LoungeClubsApi.Services;

namespace LoungeClubsApi.Features.Establishments.ReclassifyTier;

public class ReclassifyTierCommandHandler(
    AppDbContext db,
    JurisdictionService jurisdiction,
    ICurrentUserService currentUser,
    NotificationService notificationService)
    : ICommandHandler<ReclassifyTierCommand, ReclassifyTierResponse>
{
    public async Task<ReclassifyTierResponse> Handle(ReclassifyTierCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedAccessException("You must be logged in.");

        var establishment = await db.Establishments
            .Include(e => e.Owner)
            .FirstOrDefaultAsync(e => e.Id == request.EstablishmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Establishment not found.");

        if (establishment.Status != EstablishmentStatus.Approved)
            throw new InvalidOperationException($"Only approved establishments can be reclassified. Current status: {establishment.Status}.");

        var tier = await db.LicenseTiers
            .FirstOrDefaultAsync(t => t.Id == request.TierId, cancellationToken)
            ?? throw new KeyNotFoundException("License tier not found.");
        await jurisdiction.EnsureCanActOnAsync(establishment.Lga, cancellationToken);

        if (!tier.IsActive)
            throw new InvalidOperationException("The selected license tier is inactive.");

        establishment.TierId = tier.Id;

        await db.SaveChangesAsync(cancellationToken);

        await notificationService.NotifyAsync(
            establishment.OwnerId,
            NotificationType.ApplicationStatus,
            "License tier updated",
            $"The license tier of \"{establishment.Name}\" ({establishment.RegistrationNumber}) has been updated to {tier.Name}. Reason: {request.Reason}. Your levy amount may change in the next assessment cycle.",
            $"/dashboard/establishments/{establishment.Id}",
            establishment.Owner.Email,
            cancellationToken);

        return new ReclassifyTierResponse(establishment.Id, tier.Code, tier.Name);
    }
}
