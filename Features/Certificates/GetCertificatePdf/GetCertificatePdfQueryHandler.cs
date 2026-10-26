using Microsoft.Extensions.Options;
using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Configuration;
using LoungeClubsApi.Data;
using LoungeClubsApi.Interfaces;
using LoungeClubsApi.Services;

namespace LoungeClubsApi.Features.Certificates.GetCertificatePdf;

public class GetCertificatePdfQueryHandler(
    AppDbContext db,
    ICurrentUserService currentUser,
    IHttpContextAccessor httpContextAccessor,
    DocumentService documentService,
    IOptions<AppSettings> appSettings)
    : IQueryHandler<GetCertificatePdfQuery, CertificatePdfResult?>
{
    public async Task<CertificatePdfResult?> Handle(GetCertificatePdfQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("You must be logged in.");

        var certificate = await db.Certificates
            .Include(c => c.Establishment)
            .Include(c => c.LevyAssessment)
            .ThenInclude(a => a.Tier)
            .FirstOrDefaultAsync(c => c.Id == query.Id, cancellationToken);

        if (certificate is null)
            return null;

        if (certificate.Establishment.OwnerId != userId && !IsStaff())
            throw new UnauthorizedAccessException("You do not have access to this certificate.");

        var tierName = certificate.LevyAssessment.Tier?.Name ?? "Licensed";

        var content = documentService.GenerateCertificatePdf(
            certificate,
            certificate.Establishment,
            tierName,
            appSettings.Value.PortalUrl);

        return new CertificatePdfResult(content, $"{certificate.CertificateNumber}.pdf");
    }

    private bool IsStaff() => httpContextAccessor.HttpContext?.User is { } user
        && (user.IsInRole(nameof(AppRole.Admin))
            
            || user.IsInRole(nameof(AppRole.Officer))
            
            );
}
