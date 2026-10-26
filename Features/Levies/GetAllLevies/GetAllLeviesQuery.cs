using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Models.Shared;

namespace LoungeClubsApi.Features.Levies.GetAllLevies;

public record GetAllLeviesQuery(
    int? Year,
    LevyStatus? Status,
    string? Lga,
    string? EstablishmentName,
    [property: FromQuery(Name = "establishment_id")] Guid? EstablishmentId,
    [property: FromQuery(Name = "page")] int? Page,
    [property: FromQuery(Name = "per_page")] int? PageSize
) : IQuery<PaginatedResponse<LevyListItemResponse>>;
