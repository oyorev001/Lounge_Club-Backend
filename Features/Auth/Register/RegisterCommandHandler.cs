using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Entities;

namespace LoungeClubsApi.Features.Auth.Register;

public class RegisterCommandHandler(UserManager<User> userManager, RoleManager<IdentityRole> roleManager, Services.EmailVerificationService verificationService)
    : ICommandHandler<RegisterCommand>
{
    public async Task Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var userExists = await userManager.FindByEmailAsync(request.Email);
        if (userExists != null)
        {
            throw new InvalidOperationException("User already exists!");
        }

        var user = new User
        {
            Email = request.Email,
            SecurityStamp = Guid.NewGuid().ToString(),
            UserName = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.Phone,
            BusinessName = request.BusinessName,
            LockoutEnabled = true
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        var role = AppRole.BusinessOwner.ToString();
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }

        await userManager.AddToRoleAsync(user, role);

        // Send the 6-digit confirmation code — queued, non-blocking.
        await verificationService.IssueAsync(user, cancellationToken);
    }
}
