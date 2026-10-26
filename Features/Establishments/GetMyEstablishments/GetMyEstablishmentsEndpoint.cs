using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Features.Establishments.GetAllEstablishments;

namespace LoungeClubsApi.Features.Establishments.GetMyEstablishments;

public class GetMyEstablishmentsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("establishments/mine", async (IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var response = await mediator.SendQueryAsync<GetMyEstablishmentsQuery, List<EstablishmentListItem>>(new GetMyEstablishmentsQuery(), ct);
                return Results.Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
            }
        })
        .HasApiVersion(1)
        .WithName(nameof(GetMyEstablishmentsEndpoint))
        .WithTags("Establishments")
        .WithSummary("List my establishments")
        .WithDescription("Retrieves all establishments registered by the current business owner.")
        .Produces<List<EstablishmentListItem>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .HasRole(AppRole.BusinessOwner, AppRole.Admin);
    }
}
