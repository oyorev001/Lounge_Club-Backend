using LoungeClubsApi.Data;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Interfaces;
using LoungeClubsApi.Services;

namespace LoungeClubsApi.Features.Establishments.RejectEstablishment;

public class RejectEstablishmentCommandHandler(
    AppDbContext db,
    JurisdictionService jurisdiction,
    ICurrentUserService currentUser,
    NotificationService notificationService)
    : ICommandHandler<RejectEstablishmentCommand, RejectEstablishmentResponse>
{
    public async Task<RejectEstablishmentResponse> Handle(RejectEstablishmentCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedAccessException("You must be logged in.");

        var establishment = await db.Establishments
            .Include(e => e.Owner)
            .FirstOrDefaultAsync(e => e.Id == request.EstablishmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Establishment not found.");
        await jurisdiction.EnsureCanActOnAsync(establishment.Lga, cancellationToken);

        if (establishment.Status != EstablishmentStatus.PendingReview)
            throw new InvalidOperationException($"Only establishments pending review can be rejected. Current status: {establishment.Status}.");

        establishment.Status = EstablishmentStatus.Rejected;
        establishment.RejectionReason = request.Reason;

        await db.SaveChangesAsync(cancellationToken);

        await notificationService.NotifyAsync(
            establishment.OwnerId,
            NotificationType.ApplicationStatus,
            "Establishment registration rejected",
            $"Your registration of \"{establishment.Name}\" ({establishment.RegistrationNumber}) has been rejected. Reason: {request.Reason}",
            $"/dashboard/establishments/{establishment.Id}",
            establishment.Owner.Email,
            cancellationToken);

        return new RejectEstablishmentResponse(establishment.Id, establishment.Status);
    }
}
