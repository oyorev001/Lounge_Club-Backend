using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Models.Shared;

namespace LoungeClubsApi.Features.Fines.GetFines;

public record GetFinesQuery(
    FineStatus? Status,
    Guid? EstablishmentId,
    [property: FromQuery(Name = "page")] int? Page,
    [property: FromQuery(Name = "per_page")] int? PageSize
) : IQuery<PaginatedResponse<FineResponse>>;
