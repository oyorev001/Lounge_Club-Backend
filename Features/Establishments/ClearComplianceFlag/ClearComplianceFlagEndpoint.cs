using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Features.Establishments.ClearComplianceFlag;

/// <summary>
/// Officer-clears a NonCompliant flag once the underlying issue is resolved —
/// the only way out for venues flagged by critical inspections or cert
/// revocation (levy payment can't self-clear those).
/// </summary>
public class ClearComplianceFlagEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("establishments/{id:guid}/compliance-clear", async (
            Guid id,
            AppDbContext db,
            Services.JurisdictionService jurisdiction,
            CancellationToken ct) =>
        {
            var establishment = await db.Establishments.FirstOrDefaultAsync(e => e.Id == id, ct);
            if (establishment is null)
                return Results.NotFound(new { error = "Establishment not found." });
            if (establishment.Status != EstablishmentStatus.NonCompliant)
                return Results.BadRequest(new { error = $"Establishment is {establishment.Status}, not NonCompliant." });

            await jurisdiction.EnsureCanActOnAsync(establishment.Lga, ct);

            var unpaid = await db.LevyAssessments.AnyAsync(
                l => l.EstablishmentId == id && l.Status == LevyStatus.Overdue, ct);
            if (unpaid)
                return Results.BadRequest(new { error = "Overdue levies remain — the venue must pay before clearance." });

            establishment.Status = EstablishmentStatus.Approved;
            establishment.NonCompliantReason = null;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { establishment.Id, establishment.Status });
        })
        .HasApiVersion(1)
        .WithName(nameof(ClearComplianceFlagEndpoint))
        .WithTags("Establishments")
        .WithSummary("Clear a non-compliance flag after the issue is resolved")
        .RequireAuthorization()
        .HasAnyRole(AppRole.Admin, AppRole.Officer);
    }
}
