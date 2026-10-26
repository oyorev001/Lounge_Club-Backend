using System.Security.Claims;
using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Data;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Features.Establishments.GetEstablishmentById;

public class GetEstablishmentByIdQueryHandler(
    AppDbContext db,
    ICurrentUserService currentUser,
    IHttpContextAccessor httpContextAccessor,
    IFileStorage fileStorage)
    : IQueryHandler<GetEstablishmentByIdQuery, EstablishmentDetailResponse>
{
    public async Task<EstablishmentDetailResponse> Handle(GetEstablishmentByIdQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("You must be logged in.");

        var establishment = await db.Establishments
            .AsNoTracking()
            .Include(e => e.Tier)
            .Include(e => e.Owner)
            .Include(e => e.Documents)
            .FirstOrDefaultAsync(e => e.Id == query.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Establishment not found.");

        if (establishment.OwnerId != userId && !IsStaff(httpContextAccessor.HttpContext?.User))
            throw new UnauthorizedAccessException("You do not have access to this establishment.");

        var ownerName = $"{establishment.Owner.FirstName} {establishment.Owner.LastName}".Trim();
        if (string.IsNullOrEmpty(ownerName))
            ownerName = establishment.Owner.BusinessName;

        return new EstablishmentDetailResponse(
            establishment.Id,
            establishment.RegistrationNumber,
            establishment.Name,
            establishment.Type,
            establishment.AdditionalTypes,
            establishment.Description,
            establishment.Address,
            establishment.Lga,
            establishment.Landmark,
            establishment.Latitude,
            establishment.Longitude,
            establishment.ContactPhone,
            establishment.ContactEmail,
            establishment.CacNumber,
            establishment.OwnerNin,
            establishment.Capacity,
            establishment.StaffCount,
            establishment.FloorAreaSqm,
            establishment.HasLiveBand,
            establishment.HasPoolTable,
            establishment.HasRestaurant,
            establishment.HasOutdoorSpace,
            establishment.HasDj,
            establishment.HasVipArea,
            establishment.HasShisha,
            establishment.HasRooftop,
            establishment.HasLodging,
            establishment.HasParking,
            establishment.ParkingSpaces,
            establishment.OperatingHours,
            establishment.Status,
            establishment.RejectionReason,
            establishment.NonCompliantReason,
            establishment.ApprovedAt,
            establishment.ApprovedById,
            establishment.SuggestedTierScore,
            establishment.TierId,
            establishment.Tier?.Code,
            establishment.Tier?.Name,
            establishment.OwnerId,
            ownerName,
            establishment.Owner.Email,
            establishment.CreatedAt,
            await ResolveDocumentUrlsAsync(establishment, cancellationToken));
    }

    /// <summary>FileUrl holds the storage key — resolve to a usable URL (local path or presigned S3).</summary>
    private async Task<List<EstablishmentDocumentItem>> ResolveDocumentUrlsAsync(Entities.Establishment establishment, CancellationToken cancellationToken)
    {
        var docs = new List<EstablishmentDocumentItem>();
        foreach (var d in establishment.Documents.OrderByDescending(d => d.UploadedAt))
        {
            var url = await fileStorage.GetAccessUrlAsync(d.FileUrl, cancellationToken);
            docs.Add(new EstablishmentDocumentItem(d.Id, d.DocumentType, url, d.FileName, d.UploadedAt));
        }
        return docs;
    }

    private static bool IsStaff(ClaimsPrincipal? user) => user is not null && (
        user.IsInRole(nameof(AppRole.Officer)) ||
        user.IsInRole(nameof(AppRole.Admin))   );
}
