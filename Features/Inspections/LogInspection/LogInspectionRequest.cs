using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Inspections.LogInspection;

public record LogInspectionRequest(
    InspectionOutcome Outcome,
    string? Notes,
    string[]? EvidenceUrls,
    double? Latitude,
    double? Longitude);
