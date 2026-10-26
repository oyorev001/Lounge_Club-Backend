using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Establishments.GetEstablishmentById;

public class GetEstablishmentByIdEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("establishments/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var response = await mediator.SendQueryAsync<GetEstablishmentByIdQuery, EstablishmentDetailResponse>(new GetEstablishmentByIdQuery(id), ct);
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
        .WithName(nameof(GetEstablishmentByIdEndpoint))
        .WithTags("Establishments")
        .WithSummary("Get an establishment by ID")
        .WithDescription("Owners can view their own establishments; staff can view any establishment.")
        .Produces<EstablishmentDetailResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization();
    }
}
