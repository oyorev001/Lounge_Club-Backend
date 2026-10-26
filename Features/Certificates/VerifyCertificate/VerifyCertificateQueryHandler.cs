using LoungeClubsApi.Data;
using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Certificates.VerifyCertificate;

public class VerifyCertificateQueryHandler(AppDbContext db)
    : IQueryHandler<VerifyCertificateQuery, VerifyCertificateResponse>
{
    public async Task<VerifyCertificateResponse> Handle(VerifyCertificateQuery query, CancellationToken cancellationToken)
    {
        var certificate = await db.Certificates
            .Include(c => c.Establishment)
            .Include(c => c.LevyAssessment)
            .ThenInclude(a => a.Tier)
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.CertificateNumber == query.CertOrToken || c.QrToken == query.CertOrToken,
                cancellationToken);

        if (certificate is null)
        {
            return new VerifyCertificateResponse(
                false, null, null, null, null, null, null, null, null, null, null, false);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return new VerifyCertificateResponse(
            true,
            certificate.CertificateNumber,
            certificate.Establishment.Name,
            certificate.Establishment.Type,
            certificate.Establishment.Lga,
            certificate.LevyAssessment.Tier?.Name,
            certificate.LevyYear,
            certificate.IssueDate,
            certificate.ExpiryDate,
            certificate.Status,
            certificate.Establishment.Status,
            // A cert is only valid while the venue itself is operating —
            // deregistered or suspended venues can't present one as proof.
            certificate.Status == CertificateStatus.Active
                && certificate.ExpiryDate >= today
                && certificate.Establishment.Status is not (
                    EstablishmentStatus.Closed or EstablishmentStatus.Suspended));
    }
}
