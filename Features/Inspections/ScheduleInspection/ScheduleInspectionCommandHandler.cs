using LoungeClubsApi.Data;
using LoungeClubsApi.Entities;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Interfaces;
using LoungeClubsApi.Services;

namespace LoungeClubsApi.Features.Inspections.ScheduleInspection;

public class ScheduleInspectionCommandHandler(
    AppDbContext db,
    JurisdictionService jurisdiction,
    ICurrentUserService currentUser,
    NotificationService notificationService)
    : ICommandHandler<ScheduleInspectionCommand, ScheduleInspectionResponse>
{
    public async Task<ScheduleInspectionResponse> Handle(ScheduleInspectionCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("You must be logged in.");

        var establishment = await db.Establishments
            .Include(e => e.Owner)
            .FirstOrDefaultAsync(e => e.Id == request.EstablishmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Establishment not found.");
        await jurisdiction.EnsureCanActOnAsync(establishment.Lga, cancellationToken);

        // Inspecting a deregistered venue makes no sense — it can't operate.
        if (establishment.Status == EstablishmentStatus.Closed)
            throw new InvalidOperationException("Cannot schedule an inspection for a deregistered establishment.");

        var inspection = new Inspection
        {
            EstablishmentId = establishment.Id,
            OfficerId = userId,
            Type = request.Type,
            ScheduledFor = request.ScheduledFor,
            Notes = request.Notes
        };

        db.Inspections.Add(inspection);
        await db.SaveChangesAsync(cancellationToken);

        await notificationService.NotifyAsync(
            establishment.OwnerId,
            NotificationType.InspectionScheduled,
            "Inspection scheduled",
            $"A {inspection.Type.ToString().ToLowerInvariant()} inspection of \"{establishment.Name}\" has been scheduled for {inspection.ScheduledFor:dd MMM yyyy HH:mm} UTC.",
            $"/dashboard/establishments/{establishment.Id}",
            establishment.Owner.Email,
            cancellationToken);

        return new ScheduleInspectionResponse(
            inspection.Id,
            establishment.Id,
            establishment.Name,
            inspection.Type,
            inspection.ScheduledFor);
    }
}
