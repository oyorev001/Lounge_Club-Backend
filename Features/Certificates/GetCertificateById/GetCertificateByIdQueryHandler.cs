using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Features.Certificates.GetCertificateById;

public class GetCertificateByIdQueryHandler(
    AppDbContext db,
    ICurrentUserService currentUser,
    IHttpContextAccessor httpContextAccessor)
    : IQueryHandler<GetCertificateByIdQuery, CertificateDetailResponse?>
{
    public async Task<CertificateDetailResponse?> Handle(GetCertificateByIdQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("You must be logged in.");

        var certificate = await db.Certificates
            .Include(c => c.Establishment)
            .Include(c => c.LevyAssessment)
            .ThenInclude(a => a.Tier)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == query.Id, cancellationToken);

        if (certificate is null)
            return null;

        if (certificate.Establishment.OwnerId != userId && !IsStaff())
            throw new UnauthorizedAccessException("You do not have access to this certificate.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return new CertificateDetailResponse(
            certificate.Id,
            certificate.CertificateNumber,
            certificate.EstablishmentId,
            certificate.Establishment.Name,
            certificate.Establishment.Address,
            certificate.Establishment.Lga,
            certificate.Establishment.Type,
            certificate.LevyAssessment.Tier?.Name,
            certificate.LevyYear,
            certificate.AmountPaid,
            certificate.IssueDate,
            certificate.ExpiryDate,
            certificate.Status,
            certificate.Status == CertificateStatus.Active && certificate.ExpiryDate >= today,
            certificate.RevokedReason,
            certificate.RevokedAt);
    }

    private bool IsStaff() => httpContextAccessor.HttpContext?.User is { } user
        && (user.IsInRole(nameof(AppRole.Admin))
            
            || user.IsInRole(nameof(AppRole.Officer))
            
            );
}
