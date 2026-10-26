using LoungeClubsApi.Data;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Interfaces;
using LoungeClubsApi.Services;

namespace LoungeClubsApi.Features.Certificates.RevokeCertificate;

public class RevokeCertificateCommandHandler(
    AppDbContext db,
    ICurrentUserService currentUser,
    NotificationService notificationService)
    : ICommandHandler<RevokeCertificateCommand>
{
    public async Task Handle(RevokeCertificateCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("You must be logged in.");

        var certificate = await db.Certificates
            .Include(c => c.Establishment)
            .ThenInclude(e => e.Owner)
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Certificate not found.");

        if (certificate.Status != CertificateStatus.Active)
            throw new InvalidOperationException(
                $"Certificate {certificate.CertificateNumber} is not active (status: {certificate.Status}).");

        certificate.Status = CertificateStatus.Revoked;
        certificate.RevokedReason = command.Reason;
        certificate.RevokedById = userId;
        certificate.RevokedAt = DateTimeOffset.UtcNow;

        // Only flag the establishment non-compliant if no other active
        // certificate remains (e.g. a newer year's cert already issued).
        var hasOtherActiveCertificate = await db.Certificates
            .AnyAsync(c => c.EstablishmentId == certificate.EstablishmentId
                && c.Id != certificate.Id
                && c.Status == CertificateStatus.Active
                && c.ExpiryDate >= DateOnly.FromDateTime(DateTime.UtcNow),
                cancellationToken);

        if (!hasOtherActiveCertificate)
            certificate.Establishment.Status = EstablishmentStatus.NonCompliant;

        await db.SaveChangesAsync(cancellationToken);

        await notificationService.NotifyAsync(
            certificate.Establishment.OwnerId,
            NotificationType.General,
            "Compliance certificate revoked",
            $"Certificate {certificate.CertificateNumber} issued to {certificate.Establishment.Name} has been revoked. Reason: {command.Reason}",
            "/dashboard/certificates",
            certificate.Establishment.Owner?.Email,
            cancellationToken);
    }
}
