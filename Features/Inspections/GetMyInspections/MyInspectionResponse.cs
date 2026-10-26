using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Inspections.GetMyInspections;

public record MyInspectionResponse(
    Guid Id,
    Guid EstablishmentId,
    string EstablishmentName,
    InspectionType Type,
    DateTimeOffset? ScheduledFor,
    DateTimeOffset? ConductedAt,
    InspectionOutcome? Outcome,
    string? Notes,
    string? EvidenceUrls,
    string OfficerId,
    string? OfficerName);
