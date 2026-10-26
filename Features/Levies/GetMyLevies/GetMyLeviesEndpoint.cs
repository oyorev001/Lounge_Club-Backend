using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Levies.GetMyLevies;

public class GetMyLeviesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("levies/mine", async (IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var response = await mediator.SendQueryAsync<GetMyLeviesQuery, List<MyLevyResponse>>(new GetMyLeviesQuery(), ct);
                return Results.Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
            }
        })
        .HasApiVersion(1)
        .WithName(nameof(GetMyLeviesEndpoint))
        .WithTags("Levies")
        .WithSummary("Get my levy assessments")
        .WithDescription("Retrieves levy assessments for all establishments owned by the current user, newest year first.")
        .Produces<List<MyLevyResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .HasRole(AppRole.BusinessOwner, AppRole.Admin);
    }
}
