using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Models.Shared;

namespace LoungeClubsApi.Features.Establishments.GetAllEstablishments;

public class GetAllEstablishmentsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("establishments/all", async ([AsParameters] GetAllEstablishmentsRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var query = new GetAllEstablishmentsQuery(
                request.Search,
                request.Status,
                request.Lga,
                request.TierId,
                request.Type,
                request.Page,
                request.PageSize);

            var response = await mediator.SendQueryAsync<GetAllEstablishmentsQuery, PaginatedResponse<EstablishmentListItem>>(query, ct);
            return Results.Ok(response);
        })
        .HasApiVersion(1)
        .WithName(nameof(GetAllEstablishmentsEndpoint))
        .WithTags("Establishments")
        .WithSummary("List all establishments (staff)")
        .WithDescription("Retrieves a paginated, filterable list of all registered establishments.")
        .Produces<PaginatedResponse<EstablishmentListItem>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .HasPermissions(AppPermission.EstablishmentsViewAll);
    }
}
