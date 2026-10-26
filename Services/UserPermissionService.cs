using LoungeClubsApi.Attributes;
using LoungeClubsApi.Authorization.Handlers;
using LoungeClubsApi.Entities;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Services;

/// <summary>
/// Resolves a user's effective permissions from role claims in the database.
/// Used by handlers that need in-flow permission checks (tokens only carry roles).
/// </summary>
[ScopedService]
public class UserPermissionService(UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
    : IUserPermissionService
{
    public async Task<bool> HasPermissionAsync(string userId, string permission, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return false;

        var roleNames = await userManager.GetRolesAsync(user);
        foreach (var roleName in roleNames)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null) continue;

            var claims = await roleManager.GetClaimsAsync(role);
            if (claims.Any(c => c.Type == PermissionAuthorizationHandler.PermissionClaimType
                                && string.Equals(c.Value, permission, StringComparison.OrdinalIgnoreCase)))
                return true;
        }

        return false;
    }

    public async Task<bool> IsInRoleAsync(string userId, string role, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user is not null && await userManager.IsInRoleAsync(user, role);
    }
}
