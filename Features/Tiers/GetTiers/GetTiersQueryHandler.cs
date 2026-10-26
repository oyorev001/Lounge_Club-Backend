using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Data;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Features.Tiers.GetTiers;

public class GetTiersQueryHandler(
    AppDbContext db,
    ICurrentUserService currentUser,
    IHttpContextAccessor httpContextAccessor)
    : IQueryHandler<GetTiersQuery, List<TierResponse>>
{
    public async Task<List<TierResponse>> Handle(GetTiersQuery query, CancellationToken cancellationToken)
    {
        // Staff see every tier (including deactivated ones so they can be
        // reactivated); public callers only see the active set.
        var user = httpContextAccessor.HttpContext?.User;
        var isStaff = user is not null && (
            user.IsInRole(nameof(AppRole.Officer)) ||
            user.IsInRole(nameof(AppRole.Admin))   );

        return await db.LicenseTiers
            .AsNoTracking()
            .Where(t => isStaff || t.IsActive)
            .OrderBy(t => t.SortOrder)
            .Select(t => new TierResponse(
                t.Id,
                t.Code,
                t.Name,
                t.Description,
                t.AnnualFee,
                t.SortOrder,
                t.MinScore,
                t.IsActive))
            .ToListAsync(cancellationToken);
    }
}
