using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Interfaces;
using LoungeClubsApi.Services;

namespace LoungeClubsApi.Features.Complaints.ConvertToInspection;

/// <summary>Convert a complaint into a scheduled inspection for an officer.</summary>
public class ConvertComplaintToInspectionEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("complaints/{id:guid}/inspect", async (
            Guid id,
            [FromBody] ConvertRequest request,
            AppDbContext db,
            Services.JurisdictionService jurisdiction,
            ICurrentUserService currentUser,
            NotificationService notificationService,
            CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            if (userId is null)
                return Results.Unauthorized();

            var complaint = await db.Complaints
                .Include(c => c.Establishment)
                    .ThenInclude(e => e!.Owner)
                .FirstOrDefaultAsync(c => c.Id == id, ct);
            if (complaint is null)
                return Results.NotFound(new { error = "Complaint not found." });
            if (complaint.Status is not (ComplaintStatus.New or ComplaintStatus.UnderReview))
                return Results.BadRequest(new { error = $"Complaint is already {complaint.Status} — it can't be converted again." });
            if (complaint.EstablishmentId is null || complaint.Establishment is null)
                return Results.BadRequest(new { error = "Complaint is not linked to a registered establishment." });

            try
            {
                await jurisdiction.EnsureCanActOnAsync(complaint.Establishment.Lga, ct);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
            }

            var inspection = new Inspection
            {
                EstablishmentId = complaint.EstablishmentId.Value,
                OfficerId = request.OfficerId ?? userId,
                Type = InspectionType.Complaint,
                ScheduledFor = request.ScheduledFor ?? DateTimeOffset.UtcNow.AddDays(3),
                Notes = $"From complaint {complaint.Id:N} ({complaint.Category}): {complaint.Details[..Math.Min(300, complaint.Details.Length)]}"
            };
            db.Inspections.Add(inspection);

            complaint.Status = ComplaintStatus.InspectionScheduled;
            complaint.InspectionId = inspection.Id;
            await db.SaveChangesAsync(ct);

            await notificationService.NotifyAsync(
                complaint.Establishment.OwnerId,
                NotificationType.ApplicationStatus,
                "Inspection scheduled",
                $"An inspection of \"{complaint.Establishment.Name}\" has been scheduled following a filed complaint.",
                $"/dashboard/establishments/{complaint.EstablishmentId}",
                complaint.Establishment.Owner?.Email,
                ct);

            return Results.Ok(new { inspection.Id });
        })
        .HasApiVersion(1)
        .WithName(nameof(ConvertComplaintToInspectionEndpoint))
        .WithTags("Complaints")
        .RequireAuthorization()
        .HasPermissions(AppPermission.InspectionsConduct);
    }

    private record ConvertRequest(string? OfficerId, DateTimeOffset? ScheduledFor);
}
