using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Inspections.LogInspection;

public record LogInspectionCommand(
    Guid Id,
    InspectionOutcome Outcome,
    string? Notes,
    string[]? EvidenceUrls,
    double? Latitude,
    double? Longitude
) : ICommand<LogInspectionResponse>;
