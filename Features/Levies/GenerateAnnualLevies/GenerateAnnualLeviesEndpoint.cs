using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Levies.GenerateAnnualLevies;

public class GenerateAnnualLeviesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("levies/generate", async ([FromBody] GenerateAnnualLeviesCommand command, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var result = await mediator.SendCommandAsync<GenerateAnnualLeviesCommand, GenerateLeviesResponse>(command, ct);
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .HasApiVersion(1)
        .WithName(nameof(GenerateAnnualLeviesEndpoint))
        .WithTags("Levies")
        .WithSummary("Generate annual levy assessments")
        .WithDescription("Creates levy assessments for the given year for every approved establishment with an assigned tier that does not already have an assessment.")
        .Produces<GenerateLeviesResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .HasRole(AppRole.Admin);
    }
}
