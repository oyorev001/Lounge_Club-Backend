using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Establishments.CloseEstablishment;

public class CloseEstablishmentEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("establishments/{id:guid}/close", async (Guid id, [FromBody] CloseEstablishmentCommand command, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var result = await mediator.SendCommandAsync<CloseEstablishmentCommand, CloseEstablishmentResponse>(command with { EstablishmentId = id }, ct);
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
        .WithName(nameof(CloseEstablishmentEndpoint))
        .WithTags("Establishments")
        .WithSummary("Close an establishment")
        .WithDescription("Closes a live establishment (Approved, Suspended or NonCompliant) and revokes its active certificates. Owners can close their own venues; staff can close any. Outstanding levies and fines remain payable.")
        .Produces<CloseEstablishmentResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization();
    }
}
