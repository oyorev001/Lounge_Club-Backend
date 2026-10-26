using LoungeClubsApi.Data;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Interfaces;
using LoungeClubsApi.Services;

namespace LoungeClubsApi.Features.Establishments.ReinstateEstablishment;

public class ReinstateEstablishmentCommandHandler(
    AppDbContext db,
    JurisdictionService jurisdiction,
    ICurrentUserService currentUser,
    NotificationService notificationService)
    : ICommandHandler<ReinstateEstablishmentCommand, ReinstateEstablishmentResponse>
{
    public async Task<ReinstateEstablishmentResponse> Handle(ReinstateEstablishmentCommand request, CancellationToken cancellationToken)
    {
        var establishment = await db.Establishments
            .Include(e => e.Owner)
            .FirstOrDefaultAsync(e => e.Id == request.EstablishmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Establishment not found.");
        await jurisdiction.EnsureCanActOnAsync(establishment.Lga, cancellationToken);

        if (establishment.Status != EstablishmentStatus.Suspended)
            throw new InvalidOperationException($"A {establishment.Status} establishment cannot be reinstated.");

        // A suspended venue with unpaid levies isn't really compliant —
        // reinstate to NonCompliant until the money is settled.
        var unpaidLevies = await db.LevyAssessments.AnyAsync(
            l => l.EstablishmentId == establishment.Id
                && (l.Status == LevyStatus.Pending || l.Status == LevyStatus.Overdue),
            cancellationToken);
        establishment.Status = unpaidLevies
            ? EstablishmentStatus.NonCompliant
            : EstablishmentStatus.Approved;
        await db.SaveChangesAsync(cancellationToken);

        await notificationService.NotifyAsync(
            establishment.OwnerId,
            NotificationType.ApplicationStatus,
            establishment.Status == EstablishmentStatus.Approved
                ? "Suspension lifted"
                : "Suspension lifted — compliance still required",
            $"Your establishment \"{establishment.Name}\" ({establishment.RegistrationNumber}) has been reinstated and is approved to operate again. Note: certificates revoked during suspension are not re-issued — contact the ministry to renew.",
            $"/dashboard/establishments/{establishment.Id}",
            establishment.Owner.Email,
            cancellationToken,
            establishment.Owner.PhoneNumber);

        return new ReinstateEstablishmentResponse(establishment.Id, establishment.Status.ToString());
    }
}
