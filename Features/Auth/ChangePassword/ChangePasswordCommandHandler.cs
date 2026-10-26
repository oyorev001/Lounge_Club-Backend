using System.Security.Claims;
using LoungeClubsApi.Entities;

namespace LoungeClubsApi.Features.Auth.ChangePassword;

public class ChangePasswordCommandHandler(UserManager<User> userManager, IHttpContextAccessor httpContextAccessor)
    : ICommandHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found.");
        }

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Password change failed: {errors}");
        }

        // Kill any issued sessions/refresh tokens — a password change must
        // invalidate tokens that may be compromised.
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        await userManager.UpdateSecurityStampAsync(user);
    }
}
