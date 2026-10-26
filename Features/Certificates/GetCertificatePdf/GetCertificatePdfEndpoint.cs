using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Certificates.GetCertificatePdf;

public class GetCertificatePdfEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("certificates/{id:guid}/pdf", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var result = await mediator.SendQueryAsync<GetCertificatePdfQuery, CertificatePdfResult?>(new GetCertificatePdfQuery(id), ct);

                if (result is null)
                {
                    return Results.NotFound("Certificate not found.");
                }

                return Results.File(result.Content, "application/pdf", result.FileName);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
            }
        })
        .HasApiVersion(1)
        .WithName(nameof(GetCertificatePdfEndpoint))
        .WithTags("Certificates")
        .WithSummary("Download certificate PDF")
        .WithDescription("Generates and downloads the compliance certificate as a PDF. Accessible by the owning business or staff.")
        .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .HasAnyRole(
            AppRole.BusinessOwner,
            AppRole.Admin, 
            AppRole.Officer);
    }
}
