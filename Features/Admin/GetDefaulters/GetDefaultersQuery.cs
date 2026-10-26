using LoungeClubsApi.Models.Shared;

namespace LoungeClubsApi.Features.Admin.GetDefaulters;

public record GetDefaultersQuery(
    [property: FromQuery(Name = "page")] int? Page,
    [property: FromQuery(Name = "per_page")] int? PageSize
) : IQuery<PaginatedResponse<DefaulterResponse>>;
