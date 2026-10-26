namespace LoungeClubsApi.Interfaces;

public interface IUserPermissionService
{
    Task<bool> HasPermissionAsync(string userId, string permission, CancellationToken ct = default);
    Task<bool> IsInRoleAsync(string userId, string role, CancellationToken ct = default);
}
