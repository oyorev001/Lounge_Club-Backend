using LoungeClubsApi.Data;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Interfaces;
using LoungeClubsApi.Models.Shared;

namespace LoungeClubsApi.Features.Levies.GetAllLevies;

public class GetAllLeviesQueryHandler(
    AppDbContext db,
    ICurrentUserService currentUser)
    : IQueryHandler<GetAllLeviesQuery, PaginatedResponse<LevyListItemResponse>>
{
    public async Task<PaginatedResponse<LevyListItemResponse>> Handle(GetAllLeviesQuery query, CancellationToken cancellationToken)
    {
        var queryable = db.LevyAssessments.AsNoTracking();

        if (query.Year.HasValue)
        {
            queryable = queryable.Where(l => l.Year == query.Year.Value);
        }

        if (query.Status.HasValue)
        {
            queryable = queryable.Where(l => l.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Lga))
        {
            var lga = query.Lga.Trim();
            queryable = queryable.Where(l => l.Establishment.Lga == lga);
        }

        if (query.EstablishmentId.HasValue)
        {
            queryable = queryable.Where(l => l.EstablishmentId == query.EstablishmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.EstablishmentName))
        {
            var name = query.EstablishmentName.Trim();
            queryable = queryable.Where(l => l.Establishment.Name.Contains(name));
        }

        var responseQuery = queryable
            .OrderByDescending(l => l.Year)
            .ThenByDescending(l => l.CreatedAt)
            .Select(l => new LevyListItemResponse(
                l.Id,
                l.Reference,
                l.EstablishmentId,
                l.Establishment.Name,
                l.Establishment.Lga,
                l.Tier.Code,
                l.Tier.Name,
                l.Year,
                l.Amount,
                l.DueDate,
                l.Status,
                l.PaidAt,
                l.Certificate != null,
                l.Certificate != null ? l.Certificate.Id : (Guid?)null));

        var pagedList = await PagedList<LevyListItemResponse>.CreateAsync(responseQuery, query.Page ?? 1, query.PageSize ?? 15);

        return pagedList.ToPaginatedResponse(items => items, "levies");
    }
}
