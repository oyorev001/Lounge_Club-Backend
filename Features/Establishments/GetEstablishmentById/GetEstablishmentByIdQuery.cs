namespace LoungeClubsApi.Features.Establishments.GetEstablishmentById;

public record GetEstablishmentByIdQuery(Guid Id) : IQuery<EstablishmentDetailResponse>;
