using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Entities;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Features.Users.CreateUser;

public class CreateUserHandler(
    UserManager<User> userManager,
    RoleManager<IdentityRole> roleManager,
    ICurrentUserService currentUser,
    IUserPermissionService permissions) : ICommandHandler<CreateUserCommand, string>
{
    public async Task<string> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var callerId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("You must be logged in.");

        foreach (var role in command.Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                throw new InvalidOperationException($"Role '{role}' does not exist.");
        }

        var callerIsAdmin = await permissions.IsInRoleAsync(callerId, nameof(AppRole.Admin), cancellationToken);
        if (!callerIsAdmin && command.Roles.Any(r => string.Equals(r, nameof(AppRole.Admin), StringComparison.OrdinalIgnoreCase)))
            throw new UnauthorizedAccessException("Only an administrator can assign the Admin role.");

        var user = new User
        {
            UserName = command.Email,
            Email = command.Email,
            FirstName = command.FirstName,
            LastName = command.LastName,
            PhoneNumber = command.PhoneNumber,
            EmailConfirmed = true, // Auto-confirm for admin created users
            LockoutEnabled = true
        };

        var result = await userManager.CreateAsync(user, command.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(" ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"User creation failed: {errors}");
        }

        if (command.Roles.Count != 0)
        {
            var roleResult = await userManager.AddToRolesAsync(user, command.Roles);
            if (!roleResult.Succeeded)
            {
                // Rollback user creation if role assignment fails
                await userManager.DeleteAsync(user);

                var errors = string.Join(" ", roleResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Role assignment failed: {errors}");
            }
        }

        return user.Id;
    }
}
