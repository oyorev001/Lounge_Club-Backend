namespace LoungeClubsApi.Features.Users.GetCurrentUser;

public record CurrentUserResponse(
    string Id,
    string? UserName,
    string? Email,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    List<string> Roles,
    List<string> Permissions,
    bool EmailConfirmed,
    DateTimeOffset CreatedAt
);
