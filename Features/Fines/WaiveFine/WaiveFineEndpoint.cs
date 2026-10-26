using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Fines.WaiveFine;

public class WaiveFineEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("fines/{id:guid}/waive", async (Guid id, [FromBody] WaiveFineRequest body, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                await mediator.SendCommandAsync(new WaiveFineCommand(id, body.Reason), ct);
                return Results.Ok(new { Message = "Fine waived successfully." });
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
        .WithName(nameof(WaiveFineEndpoint))
        .WithTags("Fines")
        .WithSummary("Waive an unpaid fine")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .HasPermissions(AppPermission.FinesWaive);
    }
}
