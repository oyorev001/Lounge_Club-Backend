using LoungeClubsApi.Data;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Features.Fines.GetMyFines;

public class GetMyFinesQueryHandler(
    AppDbContext db,
    ICurrentUserService currentUser)
    : IQueryHandler<GetMyFinesQuery, List<MyFineResponse>>
{
    public async Task<List<MyFineResponse>> Handle(GetMyFinesQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("You must be logged in.");

        return await db.Fines
            .AsNoTracking()
            .Where(f => f.Establishment.OwnerId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new MyFineResponse(
                f.Id,
                f.Reference,
                f.EstablishmentId,
                f.Establishment.Name,
                f.FineType,
                f.Amount,
                f.Reason,
                f.Status,
                f.PaidAt,
                f.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
