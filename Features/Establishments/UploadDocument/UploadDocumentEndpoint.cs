using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Establishments.UploadDocument;

public class UploadDocumentEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("establishments/{id:guid}/documents", async (Guid id, [FromForm] IFormFile file, [FromForm] DocumentType documentType, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var result = await mediator.SendCommandAsync<UploadDocumentCommand, UploadDocumentResponse>(new UploadDocumentCommand(id, file, documentType), ct);
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
        .WithName(nameof(UploadDocumentEndpoint))
        .WithTags("Establishments")
        .WithSummary("Upload an establishment document")
        .WithDescription("Uploads a supporting document (CAC certificate, government ID, venue photo, etc.) for an establishment. Owners upload to their own establishments; staff can upload to any.")
        .Produces<UploadDocumentResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .DisableAntiforgery()
        .RequireAuthorization();
    }
}
