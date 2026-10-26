namespace LoungeClubsApi.Features.Users.ReactivateUser;

public record ReactivateUserCommand(Guid UserId) : ICommand;
