namespace LoungeClubsApi.Features.Users.GetUserRoles;

public record GetUserRolesQuery([Required] string UserId) : IQuery<List<string>>;