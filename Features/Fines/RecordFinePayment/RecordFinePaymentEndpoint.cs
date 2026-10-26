using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Fines.RecordFinePayment;

public class RecordFinePaymentEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("fines/{id:guid}/mark-paid", async (Guid id, RecordFinePaymentRequest? request, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var result = await mediator.SendCommandAsync<RecordFinePaymentCommand, RecordFinePaymentResponse>(
                    new RecordFinePaymentCommand(id, request?.Note, request?.ExternalReference), ct);
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
        .WithName(nameof(RecordFinePaymentEndpoint))
        .WithTags("Fines")
        .WithSummary("Record a manual payment for a fine")
        .HasAnyRole(AppRole.Admin);
    }
}

public record RecordFinePaymentRequest(string? Note, string? ExternalReference);
