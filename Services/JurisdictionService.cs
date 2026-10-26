using LoungeClubsApi.Attributes;
using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Data;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Services;

/// <summary>
/// Officer jurisdiction: an Officer whose account lists AssignedLgas may only
/// take enforcement actions on venues inside those LGAs. Admins and
/// un-scoped officers act statewide. BusinessOwner accounts aren't affected.
/// </summary>
[ScopedService]
public class JurisdictionService(AppDbContext db, ICurrentUserService currentUser)
{
    /// <summary>Throws UnauthorizedAccessException if the current officer may
    /// not act on a venue in <paramref name="lga"/>.</summary>
    public async Task EnsureCanActOnAsync(string? lga, CancellationToken ct = default)
    {
        if (!currentUser.IsInRole(AppRole.Officer.ToString())
            || currentUser.IsInRole(AppRole.Admin.ToString()))
            return; // Admins and owners are not jurisdiction-scoped

        var userId = currentUser.UserId;
        if (userId is null)
            return;

        var lgas = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.AssignedLgas)
            .FirstOrDefaultAsync(ct) ?? [];

        if (lgas.Count > 0
            && !string.IsNullOrWhiteSpace(lga)
            && !lgas.Any(l => string.Equals(l, lga, StringComparison.OrdinalIgnoreCase)))
        {
            throw new UnauthorizedAccessException(
                $"This venue is outside your assigned jurisdiction ({lga}). Assigned LGAs: {string.Join(", ", lgas)}.");
        }
    }
}
