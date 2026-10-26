using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Establishments.SuspendEstablishment;

public class SuspendEstablishmentEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("establishments/{id:guid}/suspend", async (Guid id, [FromBody] SuspendEstablishmentCommand command, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var result = await mediator.SendCommandAsync<SuspendEstablishmentCommand, SuspendEstablishmentResponse>(command with { EstablishmentId = id }, ct);
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
        .WithName(nameof(SuspendEstablishmentEndpoint))
        .WithTags("Establishments")
        .WithSummary("Suspend an establishment")
        .WithDescription("Suspends an establishment and revokes its active certificates. The owner is notified.")
        .Produces<SuspendEstablishmentResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .HasPermissions(AppPermission.EstablishmentsSuspend);
    }
}
