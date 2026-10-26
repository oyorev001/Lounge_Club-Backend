using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Complaints.GetComplaints;

/// <summary>Staff triage queue for public complaints.</summary>
public class GetComplaintsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("complaints", async (
            AppDbContext db,
            CancellationToken ct,
            [FromQuery(Name = "status")] ComplaintStatus? status,
            [FromQuery(Name = "category")] string? category,
            [FromQuery(Name = "establishment_id")] Guid? establishmentId,
            [FromQuery(Name = "q")] string? search,
            [FromQuery(Name = "page")] int page = 1,
            [FromQuery(Name = "per_page")] int perPage = 50) =>
        {
            page = Math.Max(1, page);
            perPage = Math.Clamp(perPage, 1, 100);
            var q = db.Complaints.AsNoTracking().AsQueryable();
            if (status is not null)
                q = q.Where(c => c.Status == status);
            if (establishmentId is not null)
                q = q.Where(c => c.EstablishmentId == establishmentId);
            if (!string.IsNullOrWhiteSpace(category))
                q = q.Where(c => c.Category == category);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = $"%{search.Trim()}%";
                q = q.Where(c =>
                    (c.Establishment != null && EF.Functions.ILike(c.Establishment.Name, s))
                    || (c.VenueDescription != null && EF.Functions.ILike(c.VenueDescription, s))
                    || EF.Functions.ILike(c.Details, s));
            }

            var total = await q.CountAsync(ct);
            var items = await q
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * perPage)
                .Take(perPage)
                .Select(c => new
                {
                    c.Id,
                    c.EstablishmentId,
                    EstablishmentName = c.Establishment != null ? c.Establishment.Name : null,
                    Lga = c.Establishment != null ? c.Establishment.Lga : null,
                    c.VenueDescription,
                    c.Category,
                    c.Details,
                    c.ComplainantName,
                    c.ComplainantPhone,
                    c.Status,
                    c.ResolutionNote,
                    c.InspectionId,
                    c.CreatedAt,
                    c.UpdatedAt
                })
                .ToListAsync(ct);

            // Status counts feed the filter chips — always over the full set
            // (not the current filters) so officers see the queue shape.
            var counts = await db.Complaints
                .GroupBy(c => c.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count, ct);

            var lastPage = Math.Max(1, (int)Math.Ceiling(total / (double)perPage));
            return Results.Ok(new
            {
                data = items,
                meta = new
                {
                    current_page = page,
                    per_page = perPage,
                    total,
                    last_page = lastPage,
                    counts,
                }
            });
        })
        .HasApiVersion(1)
        .WithName(nameof(GetComplaintsEndpoint))
        .WithTags("Complaints")
        .RequireAuthorization()
        .HasAnyPermission(AppPermission.InspectionsConduct, AppPermission.EstablishmentsViewAll);
    }
}
