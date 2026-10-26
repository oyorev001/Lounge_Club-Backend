using LoungeClubsApi.Data;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Registry;

/// <summary>
/// Public registry of licensed establishments — searchable by name/LGA.
/// Returns only what a citizen needs to verify a venue is licensed.
/// </summary>
public class SearchRegistryEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("registry", async (
            AppDbContext db,
            CancellationToken ct,
            [FromQuery(Name = "q")] string? q,
            [FromQuery(Name = "lga")] string? lga) =>
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var query = db.Establishments.AsNoTracking()
                // Registered venues the public should be able to find: approved,
                // non-compliant, and suspended (still a real venue — residents
                // reporting one shouldn't fall back to free-text). Hidden:
                // PendingReview/Rejected (not venues yet) and Closed (no longer
                // operating entities, historical record only).
                .Where(e => e.Status == EstablishmentStatus.Approved
                    || e.Status == EstablishmentStatus.NonCompliant
                    || e.Status == EstablishmentStatus.Suspended)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var needle = q.Trim().ToLower();
                query = query.Where(e => e.Name.ToLower().Contains(needle) || e.CacNumber.ToLower().Contains(needle));
            }
            if (!string.IsNullOrWhiteSpace(lga))
                query = query.Where(e => e.Lga == lga);

            var items = await query
                .OrderBy(e => e.Name)
                .Take(200)
                .Select(e => new
                {
                    e.Id,
                    e.Name,
                    e.Type,
                    e.AdditionalTypes,
                    e.Lga,
                    e.Address,
                    e.Status,
                    // Live cert validity — no owner PII
                    Certificate = e.Certificates
                        .Where(c => c.Status == CertificateStatus.Active && c.ExpiryDate >= today)
                        .OrderByDescending(c => c.ExpiryDate)
                        .Select(c => new { c.CertificateNumber, c.ExpiryDate })
                        .FirstOrDefault()
                })
                .ToListAsync(ct);

            return Results.Ok(items.Select(e => new
            {
                e.Id,
                e.Name,
                e.Type,
                e.AdditionalTypes,
                e.Lga,
                e.Address,
                status = e.Status.ToString(),
                isLicensed = e.Certificate != null,
                certificateNumber = e.Certificate?.CertificateNumber,
                validUntil = e.Certificate?.ExpiryDate
            }));
        })
        .HasApiVersion(1)
        .WithName(nameof(SearchRegistryEndpoint))
        .WithTags("Registry")
        .AllowAnonymous().RequireRateLimiting("public-read");
    }
}
