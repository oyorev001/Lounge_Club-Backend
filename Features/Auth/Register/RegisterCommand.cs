namespace LoungeClubsApi.Features.Auth.Register;

public record RegisterCommand(
    [Required, EmailAddress] string Email,
    [Required] string Password,
    [Required] string FirstName,
    [Required] string LastName,
    [Required, Phone] string Phone,
    [Required, MaxLength(200)] string BusinessName
) : ICommand;
