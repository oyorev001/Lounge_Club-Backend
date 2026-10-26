using System.Text.Json;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Interfaces;
using LoungeClubsApi.Services;

namespace LoungeClubsApi.Features.Inspections.LogAdhocInspection;

public class LogAdhocInspectionCommandHandler(
    AppDbContext db,
    JurisdictionService jurisdiction,
    ICurrentUserService currentUser,
    NotificationService notificationService)
    : ICommandHandler<LogAdhocInspectionCommand, LogAdhocInspectionResponse>
{
    public async Task<LogAdhocInspectionResponse> Handle(LogAdhocInspectionCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("You must be logged in.");

        var establishment = await db.Establishments
            .Include(e => e.Owner)
            .FirstOrDefaultAsync(e => e.Id == request.EstablishmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Establishment not found.");
        await jurisdiction.EnsureCanActOnAsync(establishment.Lga, cancellationToken);

        if (establishment.Status == EstablishmentStatus.Closed)
            throw new InvalidOperationException("Cannot log an inspection for a deregistered establishment.");

        var inspection = new Inspection
        {
            EstablishmentId = establishment.Id,
            OfficerId = userId,
            Type = request.Type,
            ScheduledFor = null,
            ConductedAt = DateTimeOffset.UtcNow,
            Outcome = request.Outcome,
            Notes = request.Notes,
            OfficerLatitude = request.Latitude,
            OfficerLongitude = request.Longitude,
            EvidenceUrls = request.EvidenceUrls is { Length: > 0 }
                ? JsonSerializer.Serialize(request.EvidenceUrls)
                : null
        };

        db.Inspections.Add(inspection);

        // Only an operating venue can be flagged non-compliant — a Critical
        // finding on a pending application or deregistered venue must not
        // move its lifecycle (a pending app would otherwise be stuck forever).
        if (request.Outcome == InspectionOutcome.Critical
            && establishment.Status == EstablishmentStatus.Approved)
            establishment.Status = EstablishmentStatus.NonCompliant;
            establishment.NonCompliantReason = "CriticalInspection";

        await db.SaveChangesAsync(cancellationToken);

        await notificationService.NotifyAsync(
            establishment.OwnerId,
            NotificationType.ApplicationStatus,
            $"Inspection completed: {inspection.Outcome}",
            $"An inspection of \"{establishment.Name}\" has been completed with outcome: {inspection.Outcome}.",
            $"/dashboard/establishments/{establishment.Id}",
            establishment.Owner.Email,
            cancellationToken);

        return new LogAdhocInspectionResponse(
            inspection.Id,
            establishment.Id,
            inspection.Outcome,
            inspection.ConductedAt);
    }
}
