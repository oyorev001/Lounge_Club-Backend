namespace LoungeClubsApi.Features.Users.UpdateUser;

public record UpdateUserCommand : ICommand
{
    public required Guid UserId { get; init; }
    [Required]
    public required string FirstName { get; init; }
    [Required]
    public required string LastName { get; init; }
    [Required]
    public required string PhoneNumber { get; init; }
    public List<string>? Roles { get; init; }
}
