using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Inspections.GetEstablishmentInspections;

public record EstablishmentInspectionResponse(
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
