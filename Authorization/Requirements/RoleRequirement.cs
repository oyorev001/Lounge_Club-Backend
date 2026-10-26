using Microsoft.AspNetCore.Authorization;

namespace LoungeClubsApi.Authorization.Requirements;

public class RoleRequirement(
    string[] allowedRoles,
    bool requireAll = false) : IAuthorizationRequirement
{
    public string[] AllowedRoles { get; } = allowedRoles;
    public bool RequireAll { get; } = requireAll;
}
