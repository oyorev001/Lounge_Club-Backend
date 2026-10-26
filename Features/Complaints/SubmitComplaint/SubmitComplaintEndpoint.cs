using LoungeClubsApi.Data;
using LoungeClubsApi.Entities;
using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Complaints.SubmitComplaint;

public record SubmitComplaintRequest(
    Guid? EstablishmentId,
    string? VenueDescription,
    string Category,
    string Details,
    string? ComplainantName,
    string? ComplainantPhone);

/// <summary>Public complaint intake — no account required.</summary>
public class SubmitComplaintEndpoint : ICarterModule
{
    private static readonly HashSet<string> Categories = new(StringComparer.OrdinalIgnoreCase)
        { "Noise", "UnderageEntry", "UnlicensedOperation", "Safety", "Other" };

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("complaints", async ([FromBody] SubmitComplaintRequest request, AppDbContext db, CancellationToken ct) =>
        {
            if (request.EstablishmentId is null && string.IsNullOrWhiteSpace(request.VenueDescription))
                return Results.BadRequest(new { error = "Select an establishment or describe the venue." });
            if (!Categories.Contains(request.Category ?? ""))
                return Results.BadRequest(new { error = "Invalid complaint category." });
            if (string.IsNullOrWhiteSpace(request.Details) || request.Details.Length > 2000)
                return Results.BadRequest(new { error = "Details are required (max 2000 characters)." });

            if (request.EstablishmentId is not null
                && !await db.Establishments.AnyAsync(e => e.Id == request.EstablishmentId.Value, ct))
                return Results.BadRequest(new { error = "Unknown establishment." });

            // Same venue + category + details within 24h is a resubmit, not a
            // new complaint — acknowledge it without creating a duplicate row.
            var details = request.Details.Trim();
            var windowStart = DateTimeOffset.UtcNow.AddHours(-24);
            var duplicate = await db.Complaints.AnyAsync(c =>
                c.CreatedAt >= windowStart
                && c.Category == request.Category.Trim()
                && c.Details == details
                && (request.EstablishmentId == null
                    ? c.VenueDescription == request.VenueDescription!.Trim()
                    : c.EstablishmentId == request.EstablishmentId), ct);
            if (duplicate)
                return Results.Ok(new
                {
                    id = Guid.Empty,
                    message = "This report matches one already received — it's with the compliance team."
                });

            var complaint = new Complaint
            {
                EstablishmentId = request.EstablishmentId,
                VenueDescription = request.VenueDescription?.Trim(),
                Category = request.Category.Trim(),
                Details = details,
                ComplainantName = request.ComplainantName?.Trim(),
                ComplainantPhone = request.ComplainantPhone?.Trim()
            };
            db.Complaints.Add(complaint);
            await db.SaveChangesAsync(ct);

            return Results.Ok(new { complaint.Id, message = "Complaint received. Our team will review it." });
        })
        .HasApiVersion(1)
        .WithName(nameof(SubmitComplaintEndpoint))
        .WithTags("Complaints")
        .AllowAnonymous()
        .RequireRateLimiting("complaint");
    }
}
