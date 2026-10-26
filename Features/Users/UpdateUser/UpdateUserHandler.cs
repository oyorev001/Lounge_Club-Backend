using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Entities;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Features.Users.UpdateUser;

public class UpdateUserHandler(
    UserManager<User> userManager,
    RoleManager<IdentityRole> roleManager,
    ICurrentUserService currentUser,
    IUserPermissionService permissions) : ICommandHandler<UpdateUserCommand>
{
    public async Task Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var callerId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("You must be logged in.");

        var user = await userManager.FindByIdAsync(command.UserId.ToString()) ?? throw new KeyNotFoundException($"User with ID {command.UserId} not found.");
        user.FirstName = command.FirstName;
        user.LastName = command.LastName;
        user.PhoneNumber = command.PhoneNumber;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"User update failed: {errors}");
        }

        if (command.Roles != null)
        {
            // Role changes need more than Users.Update — otherwise any editor
            // could grant themselves Admin.
            if (!await permissions.HasPermissionAsync(callerId, AppPermission.UsersManageRoles.GetDescription(), cancellationToken))
                throw new UnauthorizedAccessException("You do not have permission to manage user roles.");

            var callerIsAdmin = await permissions.IsInRoleAsync(callerId, nameof(AppRole.Admin), cancellationToken);
            var adminRole = nameof(AppRole.Admin);

            if (!callerIsAdmin && command.Roles.Any(r => string.Equals(r, adminRole, StringComparison.OrdinalIgnoreCase)))
                throw new UnauthorizedAccessException("Only an administrator can assign the Admin role.");

            var currentRoles = await userManager.GetRolesAsync(user);
            var rolesToAdd = command.Roles.Except(currentRoles).ToList();
            var rolesToRemove = currentRoles.Except(command.Roles).ToList();

            if (!callerIsAdmin && rolesToRemove.Any(r => string.Equals(r, adminRole, StringComparison.OrdinalIgnoreCase)))
                throw new UnauthorizedAccessException("Only an administrator can remove the Admin role.");

            if (rolesToAdd.Count != 0)
            {
                // Validate roles exist
                foreach (var role in rolesToAdd)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                        throw new InvalidOperationException($"Role '{role}' does not exist.");
                }

                var addResult = await userManager.AddToRolesAsync(user, rolesToAdd);
                if (addResult.Succeeded) await userManager.UpdateSecurityStampAsync(user);
                if (!addResult.Succeeded)
                {
                    var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to add roles: {errors}");
                }
            }

            if (rolesToRemove.Count != 0)
            {
                var removeResult = await userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (removeResult.Succeeded) await userManager.UpdateSecurityStampAsync(user);
                if (!removeResult.Succeeded)
                {
                    var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to remove roles: {errors}");
                }
            }
        }
    }
}
