using LoungeClubsApi.Data;
using LoungeClubsApi.Entities;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Models.Shared;

namespace LoungeClubsApi.Features.Establishments.GetAllEstablishments;

public class GetAllEstablishmentsQueryHandler(AppDbContext db)
    : IQueryHandler<GetAllEstablishmentsQuery, PaginatedResponse<EstablishmentListItem>>
{
    public async Task<PaginatedResponse<EstablishmentListItem>> Handle(GetAllEstablishmentsQuery query, CancellationToken cancellationToken)
    {
        IQueryable<Establishment> queryable = db.Establishments.AsNoTracking().Include(e => e.Tier);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            queryable = queryable.Where(e =>
                e.Name.Contains(search) ||
                e.RegistrationNumber.Contains(search) ||
                (e.CacNumber != null && e.CacNumber.Contains(search)));
        }

        if (query.Status.HasValue)
            queryable = queryable.Where(e => e.Status == query.Status.Value);

        if (!string.IsNullOrWhiteSpace(query.Lga))
            queryable = queryable.Where(e => e.Lga == query.Lga);

        if (query.TierId.HasValue)
            queryable = queryable.Where(e => e.TierId == query.TierId.Value);

        if (query.Type.HasValue)
            queryable = queryable.Where(e => e.Type == query.Type.Value);

        var projected = queryable
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new EstablishmentListItem(
                e.Id,
                e.RegistrationNumber,
                e.Name,
                e.Type,
                e.AdditionalTypes,
                e.Status,
                e.Lga,
                e.Address,
                e.Tier != null ? e.Tier.Code : null,
                e.Tier != null ? e.Tier.Name : null,
                e.SuggestedTierScore,
                e.CreatedAt));

        var pagedList = await PagedList<EstablishmentListItem>.CreateAsync(projected, query.Page ?? 1, query.PageSize ?? 15);

        return pagedList.ToPaginatedResponse(items => items, "establishments/all");
    }
}
