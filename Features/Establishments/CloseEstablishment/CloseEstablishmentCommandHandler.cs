using System.Security.Claims;
using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Interfaces;
using LoungeClubsApi.Services;

namespace LoungeClubsApi.Features.Establishments.CloseEstablishment;

public class CloseEstablishmentCommandHandler(
    AppDbContext db,
    JurisdictionService jurisdiction,
    ICurrentUserService currentUser,
    IHttpContextAccessor httpContextAccessor,
    NotificationService notificationService)
    : ICommandHandler<CloseEstablishmentCommand, CloseEstablishmentResponse>
{
    public async Task<CloseEstablishmentResponse> Handle(CloseEstablishmentCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("You must be logged in.");

        var establishment = await db.Establishments
            .Include(e => e.Owner)
            .FirstOrDefaultAsync(e => e.Id == request.EstablishmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Establishment not found.");
        await jurisdiction.EnsureCanActOnAsync(establishment.Lga, cancellationToken);

        if (establishment.OwnerId != userId
            && !(httpContextAccessor.HttpContext?.User.IsInRole(nameof(AppRole.Admin)) ?? false))
            throw new UnauthorizedAccessException("You do not have access to this establishment.");

        if (establishment.Status is not (EstablishmentStatus.Approved
            or EstablishmentStatus.Suspended or EstablishmentStatus.NonCompliant))
        {
            throw new InvalidOperationException(
                establishment.Status == EstablishmentStatus.Closed
                    ? "This establishment is already deregistered."
                    : $"A {establishment.Status} establishment cannot be deregistered — withdraw the application instead.");
        }

        var reason = string.IsNullOrWhiteSpace(request.Reason)
            ? "Venue deregistered"
            : request.Reason.Trim();

        establishment.Status = EstablishmentStatus.Closed;

        // A closed venue's certificates must fail public verification.
        var activeCertificates = await db.Certificates
            .Where(c => c.EstablishmentId == establishment.Id && c.Status == CertificateStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var certificate in activeCertificates)
        {
            certificate.Status = CertificateStatus.Revoked;
            certificate.RevokedReason = $"Establishment deregistered — {reason}";
            certificate.RevokedById = userId;
            certificate.RevokedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);

        var certNote = activeCertificates.Count > 0
            ? $" {activeCertificates.Count} active certificate(s) were revoked."
            : "";

        await notificationService.NotifyAsync(
            establishment.OwnerId,
            NotificationType.ApplicationStatus,
            "Establishment deregistered",
            $"Your establishment \"{establishment.Name}\" ({establishment.RegistrationNumber}) has been deregistered.{certNote} Any outstanding levies or fines remain payable.",
            $"/dashboard/establishments/{establishment.Id}",
            establishment.Owner?.Email,
            cancellationToken);

        return new CloseEstablishmentResponse(establishment.Id, establishment.Status);
    }

    private static bool IsStaff(ClaimsPrincipal? user) => user is not null && (
        user.IsInRole(nameof(AppRole.Officer)) ||
        user.IsInRole(nameof(AppRole.Admin))   );
}
