namespace LoungeClubsApi.Features.Users.UpdateProfile;

public record UpdateProfileCommand(
    string? FirstName,
    string? LastName,
    [Phone] string? PhoneNumber
) : ICommand;
