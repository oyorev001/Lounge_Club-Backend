using LoungeClubsApi.Data;
using LoungeClubsApi.Features.Establishments.GetAllEstablishments;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Features.Establishments.GetMyEstablishments;

public class GetMyEstablishmentsQueryHandler(
    AppDbContext db,
    ICurrentUserService currentUser)
    : IQueryHandler<GetMyEstablishmentsQuery, List<EstablishmentListItem>>
{
    public async Task<List<EstablishmentListItem>> Handle(GetMyEstablishmentsQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("You must be logged in.");

        return await db.Establishments
            .AsNoTracking()
            .Include(e => e.Tier)
            .Where(e => e.OwnerId == userId)
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
                e.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
