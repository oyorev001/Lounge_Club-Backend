namespace LoungeClubsApi.Features.Auth.RefreshToken;

public record RefreshTokenResponse(string AccessToken, string RefreshToken, DateTime Expiration);
