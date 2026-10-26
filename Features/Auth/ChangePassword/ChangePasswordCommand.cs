namespace LoungeClubsApi.Features.Auth.ChangePassword;

public record ChangePasswordCommand(
    [Required] string CurrentPassword,
    [Required] string NewPassword
) : ICommand;
