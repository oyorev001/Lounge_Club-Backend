using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Inspections.LogAdhocInspection;

public record LogAdhocInspectionCommand(
    Guid EstablishmentId,
    InspectionType Type,
    InspectionOutcome Outcome,
    string? Notes,
    string[]? EvidenceUrls,
    double? Latitude,
    double? Longitude
) : ICommand<LogAdhocInspectionResponse>;
