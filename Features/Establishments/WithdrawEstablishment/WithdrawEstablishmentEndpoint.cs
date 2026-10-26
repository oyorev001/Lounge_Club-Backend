using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Establishments.WithdrawEstablishment;

public class WithdrawEstablishmentEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("establishments/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var result = await mediator.SendCommandAsync<WithdrawEstablishmentCommand, WithdrawEstablishmentResponse>(new WithdrawEstablishmentCommand(id), ct);
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
        .WithName(nameof(WithdrawEstablishmentEndpoint))
        .WithTags("Establishments")
        .WithSummary("Withdraw an establishment application")
        .WithDescription("Permanently removes a PendingReview or Rejected application that has no compliance or financial records. Approved establishments must be closed instead — their history is preserved for audit.")
        .Produces<WithdrawEstablishmentResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization();
    }
}
