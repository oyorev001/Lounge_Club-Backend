namespace LoungeClubsApi.Features.Auth.Login;

public record LoginResponse(string AccessToken, string RefreshToken, DateTime Expiration);
