namespace LoungeClubsApi.Features.Auth.Login;

public record LoginCommand(
    [Required, EmailAddress] string Email,
    [Required] string Password
) : ICommand<LoginResponse>;
