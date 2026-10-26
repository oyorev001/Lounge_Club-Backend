namespace LoungeClubsApi.Features.Users.ManagePermissions;

public record GetUserPermissionsQuery([Required] string UserId) : IQuery<List<string>>;
