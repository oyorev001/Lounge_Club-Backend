namespace LoungeClubsApi.Features.Auth.ForgotPassword;

public record ForgotPasswordCommand(
    [Required, EmailAddress] string Email
) : ICommand;
