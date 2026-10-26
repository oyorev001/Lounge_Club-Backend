using LoungeClubsApi.Data;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Models.Shared;

namespace LoungeClubsApi.Features.Inspections.GetInspections;

public class GetInspectionsQueryHandler(AppDbContext db)
    : IQueryHandler<GetInspectionsQuery, PaginatedResponse<InspectionResponse>>
{
    public async Task<PaginatedResponse<InspectionResponse>> Handle(GetInspectionsQuery query, CancellationToken cancellationToken)
    {
        var queryable = db.Inspections.AsNoTracking();

        if (query.EstablishmentId.HasValue)
            queryable = queryable.Where(i => i.EstablishmentId == query.EstablishmentId.Value);

        if (query.Outcome.HasValue)
            queryable = queryable.Where(i => i.Outcome == query.Outcome.Value);

        if (!string.IsNullOrWhiteSpace(query.OfficerId))
            queryable = queryable.Where(i => i.OfficerId == query.OfficerId);

        var projected = queryable
            .OrderByDescending(i => i.ScheduledFor)
            .ThenByDescending(i => i.CreatedAt)
            .Select(i => new InspectionResponse(
                i.Id,
                i.EstablishmentId,
                i.Establishment.Name,
                i.OfficerId,
                i.Officer.FirstName == null
                    ? i.Officer.UserName
                    : (i.Officer.FirstName + " " + i.Officer.LastName).Trim(),
                i.Type,
                i.ScheduledFor,
                i.ConductedAt,
                i.Outcome,
                i.Notes,
                i.EvidenceUrls));

        var pagedList = await PagedList<InspectionResponse>.CreateAsync(projected, query.Page ?? 1, query.PageSize ?? 15);

        return pagedList.ToPaginatedResponse(items => items, "inspections");
    }
}
