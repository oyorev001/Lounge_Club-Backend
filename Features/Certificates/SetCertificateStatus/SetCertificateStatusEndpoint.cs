using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Interfaces;
using LoungeClubsApi.Services;

namespace LoungeClubsApi.Features.Certificates.SetCertificateStatus;

/// <summary>
/// Administrative status transitions on a certificate:
///   Active   → Revoked | Expired
///   Revoked  → Active (reinstate — only while still within its validity window)
///   Expired  → terminal — renewal is the only path back (automatic on levy payment).
/// Every transition requires a recorded reason and lands in the audit log.
/// </summary>
public class SetCertificateStatusEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("certificates/{id:guid}/status", async (
            Guid id,
            [FromBody] StatusRequest request,
            AppDbContext db,
            ICurrentUserService currentUser,
            NotificationService notifications,
            CancellationToken ct) =>
        {
            if (currentUser.UserId is null)
                return Results.Unauthorized();
            if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length < 5)
                return Results.BadRequest(new { error = "A reason is required (min 5 characters)." });

            var cert = await db.Certificates
                .Include(c => c.Establishment)
                .ThenInclude(e => e.Owner)
                .FirstOrDefaultAsync(c => c.Id == id, ct);
            if (cert is null)
                return Results.NotFound(new { error = "Certificate not found." });

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var reason = request.Reason.Trim();

            switch (request.Status)
            {
                case CertificateStatus.Revoked when cert.Status == CertificateStatus.Active:
                {
                    cert.Status = CertificateStatus.Revoked;
                    cert.RevokedReason = reason;
                    cert.RevokedById = currentUser.UserId;
                    cert.RevokedAt = DateTimeOffset.UtcNow;

                    var otherActive = await db.Certificates.AnyAsync(c =>
                        c.EstablishmentId == cert.EstablishmentId && c.Id != cert.Id
                        && c.Status == CertificateStatus.Active && c.ExpiryDate >= today, ct);
                    if (!otherActive && cert.Establishment.Status == EstablishmentStatus.Approved)
                        cert.Establishment.Status = EstablishmentStatus.NonCompliant;
                    break;
                }

                case CertificateStatus.Active when cert.Status == CertificateStatus.Revoked:
                {
                    if (cert.ExpiryDate < today)
                        return Results.BadRequest(new {
                            error = $"Certificate expired {cert.ExpiryDate} — renewal must come from a new levy payment.",
                        });
                    cert.Status = CertificateStatus.Active;
                    cert.RevokedReason = null;
                    cert.RevokedById = null;
                    cert.RevokedAt = null;

                    // Lift the non-compliance flag if this reinstatement makes them compliant.
                    if (cert.Establishment.Status == EstablishmentStatus.NonCompliant)
                        cert.Establishment.Status = EstablishmentStatus.Approved;
                    break;
                }

                case CertificateStatus.Expired when cert.Status == CertificateStatus.Active:
                    cert.Status = CertificateStatus.Expired;
                    var otherActiveE = await db.Certificates.AnyAsync(c =>
                        c.EstablishmentId == cert.EstablishmentId && c.Id != cert.Id
                        && c.Status == CertificateStatus.Active && c.ExpiryDate >= today, ct);
                    if (!otherActiveE && cert.Establishment.Status == EstablishmentStatus.Approved)
                        cert.Establishment.Status = EstablishmentStatus.NonCompliant;
                    break;

                default:
                    return Results.BadRequest(new {
                        error = $"Can't move a {cert.Status} certificate to {request.Status} — renewal issues a new certificate automatically.",
                    });
            }

            cert.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);

            await notifications.NotifyAsync(
                cert.Establishment.OwnerId,
                NotificationType.General,
                $"Certificate {cert.CertificateNumber} {cert.Status.ToString().ToLowerInvariant()}",
                $"Your compliance certificate status was changed to {cert.Status}: {reason}",
                emailTo: cert.Establishment.Owner?.Email,
                smsTo: cert.Establishment.Owner?.PhoneNumber,
                cancellationToken: ct);

            return Results.Ok(new { cert.Id, cert.CertificateNumber, status = cert.Status.ToString() });
        })
        .HasApiVersion(1)
        .WithName(nameof(SetCertificateStatusEndpoint))
        .WithTags("Certificates")
        .RequireAuthorization()
        .HasPermissions(AppPermission.CertificatesRevoke);
    }

    private record StatusRequest(CertificateStatus Status, string? Reason);
}
