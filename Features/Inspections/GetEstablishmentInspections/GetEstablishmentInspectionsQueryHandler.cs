using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Data;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Features.Inspections.GetEstablishmentInspections;

public class GetEstablishmentInspectionsQueryHandler(
    AppDbContext db,
    ICurrentUserService currentUser,
    IHttpContextAccessor httpContextAccessor)
    : IQueryHandler<GetEstablishmentInspectionsQuery, List<EstablishmentInspectionResponse>>
{
    public async Task<List<EstablishmentInspectionResponse>> Handle(GetEstablishmentInspectionsQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("You must be logged in.");

        var establishment = await db.Establishments
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == query.EstablishmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Establishment not found.");

        var user = httpContextAccessor.HttpContext?.User;
        var isStaff = user?.IsInRole(AppRole.Officer.ToString()) == true
            || user?.IsInRole(AppRole.Admin.ToString()) == true;

        if (establishment.OwnerId != userId && !isStaff)
            throw new UnauthorizedAccessException("You do not have access to this establishment's inspections.");

        return await db.Inspections
            .AsNoTracking()
            .Where(i => i.EstablishmentId == query.EstablishmentId)
            .OrderByDescending(i => i.ScheduledFor)
            .ThenByDescending(i => i.CreatedAt)
            .Select(i => new EstablishmentInspectionResponse(
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
