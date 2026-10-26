namespace LoungeClubsApi.Features.Inspections.GetEstablishmentInspections;

public record GetEstablishmentInspectionsQuery(Guid EstablishmentId) : IQuery<List<EstablishmentInspectionResponse>>;
