using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Certificates.RevokeCertificate;

public class RevokeCertificateEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("certificates/{id:guid}/revoke", async (Guid id, [FromBody] RevokeCertificateRequest request, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                await mediator.SendCommandAsync(new RevokeCertificateCommand(id, request.Reason), ct);
                return Results.NoContent();
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
        .WithName(nameof(RevokeCertificateEndpoint))
        .WithTags("Certificates")
        .WithSummary("Revoke a compliance certificate")
        .WithDescription("Revokes an active certificate and flags the establishment as non-compliant.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .HasPermissions(AppPermission.CertificatesRevoke);
    }
}
