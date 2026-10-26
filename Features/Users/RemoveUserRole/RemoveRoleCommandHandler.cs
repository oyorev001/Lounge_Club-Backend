using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Entities;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Features.Users.RemoveUserRole;

public class RemoveRoleCommandHandler(
    UserManager<User> userManager,
    ICurrentUserService currentUser,
    IUserPermissionService permissions) : ICommandHandler<RemoveRoleCommand>
{
    public async Task Handle(RemoveRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId) ?? throw new KeyNotFoundException("User not found.");

        // Only admins may strip the Admin role — prevents a delegated
        // role-manager from removing all administrators.
        if (string.Equals(request.RoleName, nameof(AppRole.Admin), StringComparison.OrdinalIgnoreCase)
            && !await permissions.IsInRoleAsync(currentUser.UserId!, nameof(AppRole.Admin), cancellationToken))
        {
            throw new UnauthorizedAccessException("Only an administrator can remove the Admin role.");
        }

        var result = await userManager.RemoveFromRoleAsync(user, request.RoleName);
        if (result.Succeeded) await userManager.UpdateSecurityStampAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}
