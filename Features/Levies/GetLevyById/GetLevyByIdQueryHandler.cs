using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Data;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Features.Levies.GetLevyById;

public class GetLevyByIdQueryHandler(
    AppDbContext db,
    ICurrentUserService currentUser,
    IHttpContextAccessor httpContextAccessor)
    : IQueryHandler<GetLevyByIdQuery, LevyDetailResponse>
{
    private static readonly string[] StaffRoles =
    [
        nameof(AppRole.Admin),
        nameof(AppRole.Officer),
    ];

    public async Task<LevyDetailResponse> Handle(GetLevyByIdQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("You must be logged in.");

        var levy = await db.LevyAssessments
            .AsNoTracking()
            .Include(l => l.Establishment)
            .Include(l => l.Tier)
            .Include(l => l.Transaction)
            .Include(l => l.Certificate)
            .FirstOrDefaultAsync(l => l.Id == query.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Levy assessment not found.");

        var isStaff = httpContextAccessor.HttpContext?.User is { } principal
            && StaffRoles.Any(principal.IsInRole);

        if (!isStaff && levy.Establishment.OwnerId != userId)
        {
            throw new UnauthorizedAccessException("You do not have access to this levy assessment.");
        }

        return new LevyDetailResponse(
            levy.Id,
            levy.Reference,
            levy.EstablishmentId,
            levy.Establishment.Name,
            levy.Establishment.RegistrationNumber,
            levy.Establishment.Lga,
            levy.Establishment.OwnerId,
            levy.TierId,
            levy.Tier.Code,
            levy.Tier.Name,
            levy.Year,
            levy.Amount,
            levy.DueDate,
            levy.Status,
            levy.PaidAt,
            levy.WaivedById,
            levy.WaiverReason,
            levy.Transaction?.Id,
            levy.Transaction?.Reference,
            levy.Transaction?.Status,
            levy.Certificate?.Id,
            levy.Certificate?.CertificateNumber);
    }
}
