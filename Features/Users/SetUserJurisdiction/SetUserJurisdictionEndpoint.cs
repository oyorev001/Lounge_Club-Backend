using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Data;
using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Users.SetUserJurisdiction;

/// <summary>
/// Admin sets which LGAs an officer may act on. An empty list means statewide.
/// Only meaningful for Officer-role users; admins are never scoped.
/// </summary>
public class SetUserJurisdictionEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("users/{id}/jurisdiction", async (
            string id,
            [FromBody] SetJurisdictionRequest request,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
            if (user is null)
                return Results.NotFound(new { error = "User not found." });

            var lgas = (request.Lgas ?? [])
                .Select(l => l.Trim())
                .Where(l => l.Length > 0 && l.Length <= 100)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            user.AssignedLgas = lgas;
            await db.SaveChangesAsync(ct);

            return Results.Ok(new { user.Id, user.AssignedLgas });
        })
        .HasApiVersion(1)
        .WithName(nameof(SetUserJurisdictionEndpoint))
        .WithTags("Users")
        .WithSummary("Set an officer's LGA jurisdiction (empty = statewide)")
        .RequireAuthorization()
        .HasAnyRole(AppRole.Admin);
    }

    private record SetJurisdictionRequest(List<string>? Lgas);
}
