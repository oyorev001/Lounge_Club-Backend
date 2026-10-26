using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Payments.GetPaymentStatus;

public class GetPaymentStatusEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("payments/{reference}", async (string reference, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var response = await mediator.SendQueryAsync<GetPaymentStatusQuery, PaymentStatusResponse?>(
                    new GetPaymentStatusQuery(reference), ct);

                if (response is null)
                {
                    return Results.NotFound("Transaction not found.");
                }

                return Results.Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
            }
        })
        .HasApiVersion(1)
        .WithName(nameof(GetPaymentStatusEndpoint))
        .WithTags("Payments")
        .WithSummary("Get payment status by reference")
        .WithDescription("Returns the status of a payment transaction. Accessible by the owning business or staff.")
        .Produces<PaymentStatusResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .HasAnyRole(
            AppRole.BusinessOwner,
            AppRole.Admin, 
            AppRole.Officer);
    }
}
