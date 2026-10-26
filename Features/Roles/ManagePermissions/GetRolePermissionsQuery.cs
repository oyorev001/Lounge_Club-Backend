namespace LoungeClubsApi.Features.Roles.ManagePermissions;

public record GetRolePermissionsQuery([Required] string RoleId) : IQuery<List<string>>;
