using System.Security.Claims;
using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Authorization.Handlers;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Features.Roles.ManagePermissions;

public class UpdateRolePermissionsCommandHandler(
    RoleManager<IdentityRole> roleManager,
    ICurrentUserService currentUser,
    IUserPermissionService permissions)
    : ICommandHandler<UpdateRolePermissionsCommand>
{
    private static readonly HashSet<string> ValidPermissions = Enum.GetValues<AppPermission>()
        .Select(p => p.GetDescription())
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public async Task Handle(UpdateRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        var role = await roleManager.FindByIdAsync(request.RoleId) ?? throw new KeyNotFoundException("Role not found.");

        // Editing the Admin role's permissions is a full-privilege operation.
        if (string.Equals(role.Name, nameof(AppRole.Admin), StringComparison.OrdinalIgnoreCase)
            && !await permissions.IsInRoleAsync(currentUser.UserId!, nameof(AppRole.Admin), cancellationToken))
        {
            throw new UnauthorizedAccessException("Only an administrator can modify the Admin role.");
        }

        var unknown = request.Permissions.Where(p => !ValidPermissions.Contains(p)).ToList();
        if (unknown.Count != 0)
            throw new InvalidOperationException($"Unknown permission(s): {string.Join(", ", unknown)}");

        // A non-admin caller may only grant permissions they hold themselves —
        // prevents delegated role-managers from minting super-roles.
        var isAdmin = await permissions.IsInRoleAsync(currentUser.UserId!, nameof(AppRole.Admin), cancellationToken);
        if (!isAdmin)
        {
            var escalations = new List<string>();
            foreach (var permission in request.Permissions)
            {
                if (!await permissions.HasPermissionAsync(currentUser.UserId!, permission, cancellationToken))
                    escalations.Add(permission);
            }
            if (escalations.Count != 0)
                throw new UnauthorizedAccessException(
                    $"You cannot grant permissions you do not hold: {string.Join(", ", escalations)}");
        }

        var currentClaims = await roleManager.GetClaimsAsync(role);
        var currentPermissions = currentClaims
            .Where(c => c.Type == PermissionAuthorizationHandler.PermissionClaimType)
            .ToList();

        // Remove permissions that are not in the request
        foreach (var claim in currentPermissions)
        {
            if (!request.Permissions.Contains(claim.Value))
            {
                await roleManager.RemoveClaimAsync(role, claim);
            }
        }

        // Add new permissions
        var existingPermissionValues = currentPermissions.Select(c => c.Value).ToHashSet();
        foreach (var permission in request.Permissions)
        {
            if (!existingPermissionValues.Contains(permission))
            {
                await roleManager.AddClaimAsync(role, new Claim(PermissionAuthorizationHandler.PermissionClaimType, permission));
            }
        }
    }
}
