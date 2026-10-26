namespace LoungeClubsApi.Features.Users.GetUsers;

public record UserResponse(
    string Id,
    string? UserName,
    string? Email,
    string? FirstName,
    string? LastName,
    List<string> Roles,
    List<string> Permissions,
    bool IsActive,
    List<string>? AssignedLgas = null
);