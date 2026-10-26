using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Certificates.GetCertificateById;

public class GetCertificateByIdEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("certificates/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var response = await mediator.SendQueryAsync<GetCertificateByIdQuery, CertificateDetailResponse?>(
                    new GetCertificateByIdQuery(id), ct);

                if (response is null)
                {
                    return Results.NotFound("Certificate not found.");
                }

                return Results.Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
            }
        })
        .HasApiVersion(1)
        .WithName(nameof(GetCertificateByIdEndpoint))
        .WithTags("Certificates")
        .WithSummary("Get a certificate by ID")
        .WithDescription("Returns full certificate details including establishment and tier information. Accessible by the owning business or staff.")
        .Produces<CertificateDetailResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .HasAnyRole(
            AppRole.BusinessOwner,
            AppRole.Admin, 
            AppRole.Officer);
    }
}
