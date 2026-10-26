using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Certificates.GetAllCertificates;

public class GetAllCertificatesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("certificates", async ([AsParameters] GetAllCertificatesRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.SendQueryAsync<GetAllCertificatesQuery, Models.Shared.PaginatedResponse<CertificateListItem>>(
                new GetAllCertificatesQuery(request), ct);
            return Results.Ok(result);
        })
        .HasApiVersion(1)
        .WithName(nameof(GetAllCertificatesEndpoint))
        .WithTags("Certificates")
        .WithSummary("List all certificates (staff)")
        .HasPermissions(AppPermission.CertificatesViewAll);
    }
}
