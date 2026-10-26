using System.Security.Claims;
using LoungeClubsApi.Entities;

namespace LoungeClubsApi.Interfaces;

public interface ITokenService
{
    Task<string> GenerateAccessToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}
