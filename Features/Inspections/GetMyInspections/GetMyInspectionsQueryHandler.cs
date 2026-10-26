using LoungeClubsApi.Data;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Features.Inspections.GetMyInspections;

public class GetMyInspectionsQueryHandler(
    AppDbContext db,
    ICurrentUserService currentUser)
    : IQueryHandler<GetMyInspectionsQuery, List<MyInspectionResponse>>
{
    public async Task<List<MyInspectionResponse>> Handle(GetMyInspectionsQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("You must be logged in.");

        return await db.Inspections
            .AsNoTracking()
            .Where(i => i.OfficerId == userId)
            .OrderBy(i => i.ScheduledFor)
            .Select(i => new MyInspectionResponse(
                i.Id,
                i.EstablishmentId,
                i.Establishment.Name,
                i.Type,
                i.ScheduledFor,
                i.ConductedAt,
                i.Outcome,
                i.Notes,
                i.EvidenceUrls,
                i.OfficerId,
                i.Officer.FirstName == null
                    ? i.Officer.UserName
                    : (i.Officer.FirstName + " " + i.Officer.LastName).Trim()))
            .ToListAsync(cancellationToken);
    }
}
