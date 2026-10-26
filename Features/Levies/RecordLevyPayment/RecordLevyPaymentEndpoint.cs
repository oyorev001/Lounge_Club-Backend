using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Levies.RecordLevyPayment;

public class RecordLevyPaymentEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("levies/{id:guid}/mark-paid", async (Guid id, RecordLevyPaymentRequest? request, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var result = await mediator.SendCommandAsync<RecordLevyPaymentCommand, RecordLevyPaymentResponse>(
                    new RecordLevyPaymentCommand(id, request?.Note, request?.ExternalReference), ct);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .HasApiVersion(1)
        .WithName(nameof(RecordLevyPaymentEndpoint))
        .WithTags("Levies")
        .WithSummary("Record a manual payment for a levy assessment")
        .WithDescription("Finance/Admin marks a levy as paid when payment was received outside the payment gateway. Issues the compliance certificate automatically.")
        .HasAnyRole(AppRole.Admin);
    }
}

public record RecordLevyPaymentRequest(string? Note, string? ExternalReference);
