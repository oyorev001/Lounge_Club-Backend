using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Payments.InitiateLevyPayment;

public class InitiateLevyPaymentEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("payments/levy", async ([FromBody] InitiateLevyPaymentCommand command, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var result = await mediator.SendCommandAsync<InitiateLevyPaymentCommand, InitiateLevyPaymentResponse>(command, ct);
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
        .WithName(nameof(InitiateLevyPaymentEndpoint))
        .WithTags("Payments")
        .WithSummary("Initiate a levy payment")
        .WithDescription("Creates a payment transaction for a levy assessment and returns a Paystack checkout link (card, bank transfer, or USSD).")
        .Produces<InitiateLevyPaymentResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .HasAnyRole(AppRole.BusinessOwner, AppRole.Admin, AppRole.Officer);
    }
}
