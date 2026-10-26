using LoungeClubsApi.Data;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Interfaces;
using LoungeClubsApi.Services;

namespace LoungeClubsApi.Features.Establishments.SuspendEstablishment;

public class SuspendEstablishmentCommandHandler(
    AppDbContext db,
    JurisdictionService jurisdiction,
    ICurrentUserService currentUser,
    NotificationService notificationService)
    : ICommandHandler<SuspendEstablishmentCommand, SuspendEstablishmentResponse>
{
    public async Task<SuspendEstablishmentResponse> Handle(SuspendEstablishmentCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("You must be logged in.");

        var establishment = await db.Establishments
            .Include(e => e.Owner)
            .FirstOrDefaultAsync(e => e.Id == request.EstablishmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Establishment not found.");
        await jurisdiction.EnsureCanActOnAsync(establishment.Lga, cancellationToken);

        if (establishment.Status is not (EstablishmentStatus.Approved or EstablishmentStatus.NonCompliant))
            throw new InvalidOperationException($"A {establishment.Status} establishment cannot be suspended.");

        establishment.Status = EstablishmentStatus.Suspended;

        // Revoke any active certificates so they fail public verification while suspended
        var activeCertificates = await db.Certificates
            .Where(c => c.EstablishmentId == establishment.Id && c.Status == CertificateStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var certificate in activeCertificates)
        {
            certificate.Status = CertificateStatus.Revoked;
            certificate.RevokedReason = request.Reason;
            certificate.RevokedById = userId;
            certificate.RevokedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);

        await notificationService.NotifyAsync(
            establishment.OwnerId,
            NotificationType.ApplicationStatus,
            "Establishment suspended",
            $"Your establishment \"{establishment.Name}\" ({establishment.RegistrationNumber}) has been suspended. Reason: {request.Reason}. Any active certificates have been revoked.",
            $"/dashboard/establishments/{establishment.Id}",
            establishment.Owner.Email,
            cancellationToken);

        return new SuspendEstablishmentResponse(establishment.Id, establishment.Status);
    }
}
