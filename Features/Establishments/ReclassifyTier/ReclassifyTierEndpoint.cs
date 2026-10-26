using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Establishments.ReclassifyTier;

public class ReclassifyTierEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("establishments/{id:guid}/tier", async (Guid id, [FromBody] ReclassifyTierCommand command, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var result = await mediator.SendCommandAsync<ReclassifyTierCommand, ReclassifyTierResponse>(command with { EstablishmentId = id }, ct);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
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
        .WithName(nameof(ReclassifyTierEndpoint))
        .WithTags("Establishments")
        .WithSummary("Reclassify an establishment's license tier")
        .WithDescription("Assigns a new license tier to an approved establishment. The owner is notified that the levy may change in the next cycle.")
        .Produces<ReclassifyTierResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .HasPermissions(AppPermission.EstablishmentsAssignTier);
    }
}
