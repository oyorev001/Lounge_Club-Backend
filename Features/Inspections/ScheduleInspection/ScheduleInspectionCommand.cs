using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Inspections.ScheduleInspection;

public record ScheduleInspectionCommand(
    Guid EstablishmentId,
    InspectionType Type,
    DateTimeOffset ScheduledFor,
    string? Notes
) : ICommand<ScheduleInspectionResponse>;
