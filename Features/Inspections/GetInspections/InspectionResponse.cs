using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Inspections.GetInspections;

public record InspectionResponse(
    Guid Id,
    Guid EstablishmentId,
    string EstablishmentName,
    string OfficerId,
    string? OfficerName,
    InspectionType Type,
    DateTimeOffset? ScheduledFor,
    DateTimeOffset? ConductedAt,
    InspectionOutcome? Outcome,
    string? Notes,
    string? EvidenceUrls);
