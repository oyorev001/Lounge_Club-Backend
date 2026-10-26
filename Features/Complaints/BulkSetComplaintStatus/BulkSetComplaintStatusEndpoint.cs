using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Features.Complaints.BulkSetComplaintStatus;

/// <summary>
/// Bulk triage — residents file the same report repeatedly, so officers need
/// one action that clears a whole group of identical complaints.
/// </summary>
public class BulkSetComplaintStatusEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("complaints/bulk-status", async (
            [FromBody] BulkRequest request,
            AppDbContext db,
            ICurrentUserService currentUser,
            CancellationToken ct) =>
        {
            if (currentUser.UserId is null)
                return Results.Unauthorized();
            if (request.Status is not (ComplaintStatus.Resolved or ComplaintStatus.Dismissed or ComplaintStatus.UnderReview))
                return Results.BadRequest(new { error = "Status must be Resolved, Dismissed or UnderReview." });

            var q = db.Complaints.Where(c =>
                c.Status != ComplaintStatus.Resolved && c.Status != ComplaintStatus.Dismissed);

            if (request.AllMatching)
            {
                // Same filters as the list endpoint — "clear everything shown".
                if (request.FromStatus is not null)
                    q = q.Where(c => c.Status == request.FromStatus);
                if (!string.IsNullOrWhiteSpace(request.Category))
                    q = q.Where(c => c.Category == request.Category.Trim());
                if (!string.IsNullOrWhiteSpace(request.Q))
                {
                    var s = $"%{request.Q.Trim()}%";
                    q = q.Where(c =>
                        (c.Establishment != null && EF.Functions.ILike(c.Establishment.Name, s))
                        || (c.VenueDescription != null && EF.Functions.ILike(c.VenueDescription, s))
                        || EF.Functions.ILike(c.Details, s));
                }
            }
            else
            {
                if (request.Ids is null || request.Ids.Count == 0 || request.Ids.Count > 200)
                    return Results.BadRequest(new { error = "Provide 1–200 complaint ids, or allMatching with filters." });
                q = q.Where(c => request.Ids.Contains(c.Id));
            }

            var complaints = await q.ToListAsync(ct);

            foreach (var complaint in complaints)
            {
                complaint.Status = request.Status;
                complaint.ResolutionNote = request.Note?.Trim();
            }
            await db.SaveChangesAsync(ct);

            return Results.Ok(new { updated = complaints.Count });
        })
        .HasApiVersion(1)
        .WithName(nameof(BulkSetComplaintStatusEndpoint))
        .WithTags("Complaints")
        .RequireAuthorization()
        .HasAnyPermission(AppPermission.InspectionsConduct, AppPermission.EstablishmentsViewAll);
    }

    private record BulkRequest(
        List<Guid>? Ids,
        ComplaintStatus Status,
        string? Note,
        bool AllMatching = false,
        ComplaintStatus? FromStatus = null,
        string? Category = null,
        string? Q = null);
}
