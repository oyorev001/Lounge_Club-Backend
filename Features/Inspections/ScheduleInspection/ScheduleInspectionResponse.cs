using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Inspections.ScheduleInspection;

public record ScheduleInspectionResponse(
    Guid Id,
    Guid EstablishmentId,
    string EstablishmentName,
    InspectionType Type,
    DateTimeOffset? ScheduledFor);
