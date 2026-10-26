using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Entities;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Features.Users.AssignUserRole;

public class AssignRoleCommandHandler(
    UserManager<User> userManager,
    RoleManager<IdentityRole> roleManager,
    ICurrentUserService currentUser,
    IUserPermissionService permissions)
    : ICommandHandler<AssignRoleCommand>
{
    public async Task Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId) ?? throw new KeyNotFoundException("User not found.");
        if (!await roleManager.RoleExistsAsync(request.RoleName))
        {
            throw new InvalidOperationException("Role does not exist.");
        }

        // The Admin role grants every permission — only admins may bestow it.
        if (string.Equals(request.RoleName, nameof(AppRole.Admin), StringComparison.OrdinalIgnoreCase)
            && !await permissions.IsInRoleAsync(currentUser.UserId!, nameof(AppRole.Admin), cancellationToken))
        {
            throw new UnauthorizedAccessException("Only an administrator can assign the Admin role.");
        }

        var result = await userManager.AddToRoleAsync(user, request.RoleName);
        if (result.Succeeded) await userManager.UpdateSecurityStampAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}
