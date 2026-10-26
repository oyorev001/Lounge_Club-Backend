using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Inspections.LogAdhocInspection;

public record LogAdhocInspectionResponse(
    Guid Id,
    Guid EstablishmentId,
    InspectionOutcome? Outcome,
    DateTimeOffset? ConductedAt);
