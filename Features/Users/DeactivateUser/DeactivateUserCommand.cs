namespace LoungeClubsApi.Features.Users.DeactivateUser;

public record DeactivateUserCommand(Guid UserId) : ICommand;
