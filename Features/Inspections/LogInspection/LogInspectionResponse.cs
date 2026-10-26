using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Inspections.LogInspection;

public record LogInspectionResponse(
    Guid Id,
    Guid EstablishmentId,
    InspectionOutcome? Outcome,
    DateTimeOffset? ConductedAt);
