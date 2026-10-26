using Microsoft.AspNetCore.Authorization;

namespace LoungeClubsApi.Authorization.Requirements;

public class PermissionRequirement(
    string[] requiredPermissions,
    bool requireAll = true) : IAuthorizationRequirement
{
    public string[] RequiredPermissions { get; } = requiredPermissions;
    public bool RequireAll { get; } = requireAll;
}
