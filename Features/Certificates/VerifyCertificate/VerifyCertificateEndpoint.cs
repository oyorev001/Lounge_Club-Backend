namespace LoungeClubsApi.Features.Certificates.VerifyCertificate;

public class VerifyCertificateEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("verify/{certOrToken}", async (string certOrToken, IMediator mediator, CancellationToken ct) =>
        {
            var response = await mediator.SendQueryAsync<VerifyCertificateQuery, VerifyCertificateResponse>(
                new VerifyCertificateQuery(certOrToken), ct);

            return Results.Ok(response);
        })
        .HasApiVersion(1)
        .WithName(nameof(VerifyCertificateEndpoint))
        .WithTags("Certificates")
        .WithSummary("Verify a compliance certificate")
        .WithDescription("Public verification of a certificate by certificate number or QR token.")
        .Produces<VerifyCertificateResponse>(StatusCodes.Status200OK)
        .AllowAnonymous().RequireRateLimiting("public-read");
    }
}
