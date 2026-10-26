using LoungeClubsApi.Data;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Features.Certificates.GetMyCertificates;

public class GetMyCertificatesQueryHandler(
    AppDbContext db,
    ICurrentUserService currentUser)
    : IQueryHandler<GetMyCertificatesQuery, List<MyCertificateResponse>>
{
    public async Task<List<MyCertificateResponse>> Handle(GetMyCertificatesQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("You must be logged in.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var certificates = await db.Certificates
            .Include(c => c.Establishment)
            .Include(c => c.LevyAssessment)
            .ThenInclude(a => a.Tier)
            .Where(c => c.Establishment.OwnerId == userId)
            .OrderByDescending(c => c.IssueDate)
            .ToListAsync(cancellationToken);

        return certificates.Select(c => new MyCertificateResponse(
            c.Id,
            c.CertificateNumber,
            c.Establishment.Name,
            c.LevyAssessment.Tier?.Name,
            c.LevyYear,
            c.AmountPaid,
            c.IssueDate,
            c.ExpiryDate,
            c.Status,
            c.Status == CertificateStatus.Active && c.ExpiryDate >= today)).ToList();
    }
}
