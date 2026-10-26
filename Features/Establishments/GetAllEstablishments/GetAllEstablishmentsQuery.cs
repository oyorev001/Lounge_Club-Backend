using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Models.Shared;

namespace LoungeClubsApi.Features.Establishments.GetAllEstablishments;

public record GetAllEstablishmentsQuery(
    string? Search,
    EstablishmentStatus? Status,
    string? Lga,
    Guid? TierId,
    EstablishmentType? Type,
    int? Page,
    int? PageSize
) : IQuery<PaginatedResponse<EstablishmentListItem>>;
