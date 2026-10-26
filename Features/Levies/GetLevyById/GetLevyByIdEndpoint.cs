using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Levies.GetLevyById;

public class GetLevyByIdEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("levies/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var response = await mediator.SendQueryAsync<GetLevyByIdQuery, LevyDetailResponse>(new GetLevyByIdQuery(id), ct);
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
        .WithName(nameof(GetLevyByIdEndpoint))
        .WithTags("Levies")
        .WithSummary("Get a levy assessment by ID")
        .WithDescription("Retrieves full details of a levy assessment. Accessible to the owning business owner and staff.")
        .Produces<LevyDetailResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .HasAnyRole(AppRole.BusinessOwner, AppRole.Admin, AppRole.Officer);
    }
}
