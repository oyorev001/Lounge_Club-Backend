using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Payments.InitiateFinePayment;

public class InitiateFinePaymentEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("payments/fine", async ([FromBody] InitiateFinePaymentCommand command, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var result = await mediator.SendCommandAsync<InitiateFinePaymentCommand, InitiateFinePaymentResponse>(command, ct);
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
        .WithName(nameof(InitiateFinePaymentEndpoint))
        .WithTags("Payments")
        .WithSummary("Initiate a fine payment")
        .WithDescription("Creates a payment transaction for a regulatory fine and returns a Paystack checkout link (card, bank transfer, or USSD).")
        .Produces<InitiateFinePaymentResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .HasAnyRole(AppRole.BusinessOwner, AppRole.Admin, AppRole.Officer);
    }
}
