using LoungeClubsApi.Models.Shared;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;

namespace LoungeClubsApi.Features.Users.GetUsers;

public record GetUsersQuery(
    string? Search,
    [property: FromQuery(Name = "role")] string? Role,
    /// <summary>Hide accounts whose roles are all stale/deleted (e.g. legacy
    /// "Finance" roles removed in the role simplification).</summary>
    [property: FromQuery(Name = "current_roles_only")] bool? CurrentRolesOnly,
    UserSortColumn? SortColumn,
    SortOrder? SortOrder,
    [property: FromQuery(Name = "page")] int? Page,
    [property: FromQuery(Name = "per_page")] int? PageSize
) : IQuery<PaginatedResponse<UserResponse>>;
