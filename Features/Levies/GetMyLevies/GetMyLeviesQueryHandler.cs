using LoungeClubsApi.Data;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Features.Levies.GetMyLevies;

public class GetMyLeviesQueryHandler(
    AppDbContext db,
    ICurrentUserService currentUser)
    : IQueryHandler<GetMyLeviesQuery, List<MyLevyResponse>>
{
    public async Task<List<MyLevyResponse>> Handle(GetMyLeviesQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("You must be logged in.");

        return await db.LevyAssessments
            .AsNoTracking()
            .Where(l => l.Establishment.OwnerId == userId)
            .OrderByDescending(l => l.Year)
            .Select(l => new MyLevyResponse(
                l.Id,
                l.Reference,
                l.EstablishmentId,
                l.Establishment.Name,
                l.Tier.Code,
                l.Tier.Name,
                l.Year,
                l.Amount,
                l.DueDate,
                l.Status,
                l.PaidAt,
                l.Certificate != null,
                l.Certificate != null ? l.Certificate.Id : (Guid?)null))
            .ToListAsync(cancellationToken);
    }
}
