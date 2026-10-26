using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Inspections.GetEstablishmentInspections;

public class GetEstablishmentInspectionsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("establishments/{id:guid}/inspections", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var response = await mediator.SendQueryAsync<GetEstablishmentInspectionsQuery, List<EstablishmentInspectionResponse>>(
                    new GetEstablishmentInspectionsQuery(id), ct);
                return Results.Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
            }
        })
        .HasApiVersion(1)
        .WithName(nameof(GetEstablishmentInspectionsEndpoint))
        .WithTags("Inspections")
        .WithSummary("List inspections for an establishment")
        .WithDescription("Accessible to the establishment owner and staff roles.")
        .Produces<List<EstablishmentInspectionResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .HasRole(AppRole.BusinessOwner, AppRole.Officer, AppRole.Admin);
    }
}
