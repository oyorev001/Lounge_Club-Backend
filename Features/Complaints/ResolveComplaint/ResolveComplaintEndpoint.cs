using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Features.Complaints.ResolveComplaint;

/// <summary>Mark a complaint resolved or dismissed.</summary>
public class ResolveComplaintEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("complaints/{id:guid}/resolve", async (
            Guid id,
            [FromBody] ResolveRequest request,
            AppDbContext db,
            ICurrentUserService currentUser,
            CancellationToken ct) =>
        {
            if (currentUser.UserId is null)
                return Results.Unauthorized();

            var complaint = await db.Complaints.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (complaint is null)
                return Results.NotFound(new { error = "Complaint not found." });
            if (complaint.Status is ComplaintStatus.Resolved or ComplaintStatus.Dismissed)
                return Results.BadRequest(new { error = $"Complaint is already {complaint.Status} — terminal." });
            if (request.Status is not (ComplaintStatus.Resolved or ComplaintStatus.Dismissed or ComplaintStatus.UnderReview))
                return Results.BadRequest(new { error = "Status must be Resolved, Dismissed or UnderReview." });

            complaint.Status = request.Status;
            complaint.ResolutionNote = request.Note?.Trim();
            await db.SaveChangesAsync(ct);

            return Results.Ok(new { complaint.Id, complaint.Status });
        })
        .HasApiVersion(1)
        .WithName(nameof(ResolveComplaintEndpoint))
        .WithTags("Complaints")
        .RequireAuthorization()
        .HasAnyPermission(AppPermission.InspectionsConduct, AppPermission.EstablishmentsViewAll);
    }

    private record ResolveRequest(ComplaintStatus Status, string? Note);
}
