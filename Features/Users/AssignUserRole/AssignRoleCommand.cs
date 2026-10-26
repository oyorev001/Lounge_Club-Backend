namespace LoungeClubsApi.Features.Users.AssignUserRole;

public record AssignRoleCommand(
    [Required] string UserId,
    [Required][MaxLength(50)] string RoleName
) : ICommand;