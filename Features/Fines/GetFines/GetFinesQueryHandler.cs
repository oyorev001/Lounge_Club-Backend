using LoungeClubsApi.Data;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Models.Shared;

namespace LoungeClubsApi.Features.Fines.GetFines;

public class GetFinesQueryHandler(AppDbContext db)
    : IQueryHandler<GetFinesQuery, PaginatedResponse<FineResponse>>
{
    public async Task<PaginatedResponse<FineResponse>> Handle(GetFinesQuery query, CancellationToken cancellationToken)
    {
        var queryable = db.Fines.AsNoTracking();

        if (query.Status.HasValue)
            queryable = queryable.Where(f => f.Status == query.Status.Value);

        if (query.EstablishmentId.HasValue)
            queryable = queryable.Where(f => f.EstablishmentId == query.EstablishmentId.Value);

        var projected = queryable
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new FineResponse(
                f.Id,
                f.Reference,
                f.EstablishmentId,
                f.Establishment.Name,
                f.IssuedById,
                f.IssuedBy.FirstName == null
                    ? f.IssuedBy.UserName
                    : (f.IssuedBy.FirstName + " " + f.IssuedBy.LastName).Trim(),
                f.FineType,
                f.Amount,
                f.Reason,
                f.Status,
                f.InspectionId,
                f.PaidAt,
                f.CreatedAt));

        var pagedList = await PagedList<FineResponse>.CreateAsync(projected, query.Page ?? 1, query.PageSize ?? 15);

        return pagedList.ToPaginatedResponse(items => items, "fines");
    }
}
