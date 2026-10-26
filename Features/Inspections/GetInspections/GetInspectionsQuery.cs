using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Models.Shared;

namespace LoungeClubsApi.Features.Inspections.GetInspections;

public record GetInspectionsQuery(
    Guid? EstablishmentId,
    InspectionOutcome? Outcome,
    string? OfficerId,
    [property: FromQuery(Name = "page")] int? Page,
    [property: FromQuery(Name = "per_page")] int? PageSize
) : IQuery<PaginatedResponse<InspectionResponse>>;
