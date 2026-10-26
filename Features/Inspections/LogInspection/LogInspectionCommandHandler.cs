using System.Text.Json;
using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Interfaces;
using LoungeClubsApi.Services;

namespace LoungeClubsApi.Features.Inspections.LogInspection;

public class LogInspectionCommandHandler(
    AppDbContext db,
    JurisdictionService jurisdiction,
    ICurrentUserService currentUser,
    IHttpContextAccessor httpContextAccessor,
    NotificationService notificationService)
    : ICommandHandler<LogInspectionCommand, LogInspectionResponse>
{
    public async Task<LogInspectionResponse> Handle(LogInspectionCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("You must be logged in.");

        var inspection = await db.Inspections
            .Include(i => i.Establishment)
                .ThenInclude(e => e.Owner)
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Inspection not found.");
        await jurisdiction.EnsureCanActOnAsync(inspection.Establishment.Lga, cancellationToken);

        // Can't complete an inspection on a venue that's already deregistered.
        if (inspection.Establishment.Status == EstablishmentStatus.Closed)
            throw new InvalidOperationException("Cannot log an inspection for a deregistered establishment.");

        if (inspection.ConductedAt != null)
            throw new InvalidOperationException("This inspection has already been completed.");

        // Only the assigned officer (or Admin/DG) may complete an inspection —
        // prevents one officer sabotaging inspections scheduled by another.
        var user = httpContextAccessor.HttpContext?.User;
        var isSupervisor = user?.IsInRole(nameof(AppRole.Admin)) == true;
        if (inspection.OfficerId != userId && !isSupervisor)
            throw new UnauthorizedAccessException("This inspection is assigned to a different officer.");

        inspection.ConductedAt = DateTimeOffset.UtcNow;
        inspection.Outcome = request.Outcome;
        inspection.Notes = request.Notes ?? inspection.Notes;
        inspection.OfficerLatitude = request.Latitude;
        inspection.OfficerLongitude = request.Longitude;
        inspection.EvidenceUrls = request.EvidenceUrls is { Length: > 0 }
            ? JsonSerializer.Serialize(request.EvidenceUrls)
            : null;

        // Same guard — Critical flips only Approved venues (see adhoc handler).
        if (request.Outcome == InspectionOutcome.Critical
            && inspection.Establishment.Status == EstablishmentStatus.Approved)
            inspection.Establishment.Status = EstablishmentStatus.NonCompliant;
            inspection.Establishment.NonCompliantReason = "CriticalInspection";

        await db.SaveChangesAsync(cancellationToken);

        await notificationService.NotifyAsync(
            inspection.Establishment.OwnerId,
            NotificationType.ApplicationStatus,
            $"Inspection completed: {inspection.Outcome}",
            $"The inspection of \"{inspection.Establishment.Name}\" has been completed with outcome: {inspection.Outcome}.",
            $"/dashboard/establishments/{inspection.EstablishmentId}",
            inspection.Establishment.Owner.Email,
            cancellationToken);

        return new LogInspectionResponse(
            inspection.Id,
            inspection.EstablishmentId,
            inspection.Outcome,
            inspection.ConductedAt);
    }
}
