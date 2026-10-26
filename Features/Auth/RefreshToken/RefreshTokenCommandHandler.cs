using LoungeClubsApi.Entities;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Features.Auth.RefreshToken;

public class RefreshTokenCommandHandler(UserManager<User> userManager, ITokenService tokenService)
    : ICommandHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var principal = tokenService.GetPrincipalFromExpiredToken(request.AccessToken) ?? throw new UnauthorizedAccessException("Invalid access token or refresh token");
        var username = principal.Identity?.Name;

        var user = await userManager.FindByNameAsync(username!);

        if (user is null || user.RefreshToken is null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            throw new UnauthorizedAccessException("Invalid access token or refresh token");

        if (user.RefreshToken != Login.RefreshTokenHasher.Hash(request.RefreshToken))
        {
            // A well-formed token that doesn't match the stored hash means the
            // previous token was stolen and replayed — kill the whole session.
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await userManager.UpdateSecurityStampAsync(user);
            await userManager.UpdateAsync(user);
            throw new UnauthorizedAccessException("Invalid access token or refresh token");
        }

        var newAccessToken = await tokenService.GenerateAccessToken(user);
        var newRefreshToken = tokenService.GenerateRefreshToken();

        user.RefreshToken = Login.RefreshTokenHasher.Hash(newRefreshToken);
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        await userManager.UpdateAsync(user);

        return new RefreshTokenResponse(newAccessToken, newRefreshToken, DateTime.UtcNow.AddDays(7));
    }
}
