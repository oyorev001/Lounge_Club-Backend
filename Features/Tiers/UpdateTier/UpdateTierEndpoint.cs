using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Features.Tiers.GetTiers;

namespace LoungeClubsApi.Features.Tiers.UpdateTier;

public class UpdateTierEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("tiers/{id:guid}", async (Guid id, [FromBody] UpdateTierCommand command, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var result = await mediator.SendCommandAsync<UpdateTierCommand, TierResponse>(command with { Id = id }, ct);
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
        .WithName(nameof(UpdateTierEndpoint))
        .WithTags("Tiers")
        .WithSummary("Update a license tier")
        .WithDescription("Updates the details of an existing license tier, including its active flag.")
        .Produces<TierResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .HasPermissions(AppPermission.TiersManage);
    }
}
