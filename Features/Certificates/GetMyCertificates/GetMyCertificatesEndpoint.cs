using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Certificates.GetMyCertificates;

public class GetMyCertificatesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("certificates/mine", async (IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var response = await mediator.SendQueryAsync<GetMyCertificatesQuery, List<MyCertificateResponse>>(
                    new GetMyCertificatesQuery(), ct);
                return Results.Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
            }
        })
        .HasApiVersion(1)
        .WithName(nameof(GetMyCertificatesEndpoint))
        .WithTags("Certificates")
        .WithSummary("List my compliance certificates")
        .WithDescription("Returns all compliance certificates issued to establishments owned by the caller.")
        .Produces<List<MyCertificateResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .HasRole(AppRole.BusinessOwner);
    }
}
