using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Establishments.ApproveEstablishment;

public class ApproveEstablishmentEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("establishments/{id:guid}/approve", async (Guid id, [FromBody] ApproveEstablishmentCommand command, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var result = await mediator.SendCommandAsync<ApproveEstablishmentCommand, ApproveEstablishmentResponse>(command with { EstablishmentId = id }, ct);
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
        .WithName(nameof(ApproveEstablishmentEndpoint))
        .WithTags("Establishments")
        .WithSummary("Approve an establishment")
        .WithDescription("Approves a pending or rejected registration, binds the officer-assigned license tier, and generates the current year's levy assessment.")
        .Produces<ApproveEstablishmentResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .HasPermissions(AppPermission.EstablishmentsReview);
    }
}
