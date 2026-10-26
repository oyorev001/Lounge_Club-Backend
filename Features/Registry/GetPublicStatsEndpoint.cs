using LoungeClubsApi.Data;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Registry;

/// <summary>
/// Public homepage counters — licensed venues, active certificates, LGAs
/// covered. Anonymous + rate-limited like the rest of the registry.
/// </summary>
public class GetPublicStatsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("public/stats", async (AppDbContext db, CancellationToken ct) =>
        {
            var venues = await db.Establishments.CountAsync(
                e => e.Status == EstablishmentStatus.Approved, ct);
            var certificates = await db.Certificates.CountAsync(
                c => c.Status == CertificateStatus.Active, ct);
            var lgas = await db.Establishments
                .Where(e => e.Status == EstablishmentStatus.Approved)
                .Select(e => e.Lga)
                .Distinct()
                .CountAsync(ct);

            return Results.Ok(new { venues, certificates, lgas });
        })
        .HasApiVersion(1)
        .WithName(nameof(GetPublicStatsEndpoint))
        .WithTags("Registry")
        .AllowAnonymous()
        .RequireRateLimiting("public-read");
    }
}
