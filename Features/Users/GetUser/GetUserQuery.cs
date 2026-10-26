using LoungeClubsApi.Features.Users.GetUsers;

namespace LoungeClubsApi.Features.Users.GetUser;

public record GetUserQuery([Required] string Id) : IQuery<UserResponse?>;
