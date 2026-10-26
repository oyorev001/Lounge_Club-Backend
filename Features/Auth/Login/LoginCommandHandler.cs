using LoungeClubsApi.Entities;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Features.Auth.Login;

public class LoginCommandHandler(UserManager<User> userManager, ITokenService tokenService)
    : ICommandHandler<LoginCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);

        if (user is not null
            && user.LockoutEnd is not null
            && user.LockoutEnd > DateTimeOffset.UtcNow)
        {
            throw new UnauthorizedAccessException("This account has been deactivated. Contact the administrator.");
        }

        if (user == null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            // Count failures so repeated brute-force attempts lock the account.
            if (user is { LockoutEnabled: true })
                await userManager.AccessFailedAsync(user);

            throw new UnauthorizedAccessException("Invalid credentials");
        }

        if (user.LockoutEnabled && user.AccessFailedCount > 0)
            await userManager.ResetAccessFailedCountAsync(user);

        var accessToken = await tokenService.GenerateAccessToken(user);
        var refreshToken = tokenService.GenerateRefreshToken();

        user.RefreshToken = RefreshTokenHasher.Hash(refreshToken);
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        await userManager.UpdateAsync(user);

        return new LoginResponse(accessToken, refreshToken, DateTime.UtcNow.AddDays(7));
    }
}

    internal static class RefreshTokenHasher
    {
        public static string Hash(string token) =>
            Convert.ToBase64String(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(token)));
    }
