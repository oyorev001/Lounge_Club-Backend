using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Payments.GetReceiptPdf;

public class GetReceiptPdfEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("payments/{id:guid}/receipt", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var result = await mediator.SendQueryAsync<GetReceiptPdfQuery, ReceiptPdfResult?>(new GetReceiptPdfQuery(id), ct);

                if (result is null)
                {
                    return Results.NotFound("Transaction not found.");
                }

                return Results.File(result.Content, "application/pdf", result.FileName);
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
        .WithName(nameof(GetReceiptPdfEndpoint))
        .WithTags("Payments")
        .WithSummary("Download payment receipt PDF")
        .WithDescription("Generates and downloads the official payment receipt for a successful transaction.")
        .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .HasAnyRole(
            AppRole.BusinessOwner,
            AppRole.Admin, 
            AppRole.Officer);
    }
}
