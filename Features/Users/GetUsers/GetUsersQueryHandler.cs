using LoungeClubsApi.Data;
using LoungeClubsApi.Entities;
using LoungeClubsApi.Models.Shared;

namespace LoungeClubsApi.Features.Users.GetUsers;

public class GetUsersQueryHandler(UserManager<User> userManager, AppDbContext db) : IQueryHandler<GetUsersQuery, PaginatedResponse<UserResponse>>
{
    public async Task<PaginatedResponse<UserResponse>> Handle(GetUsersQuery query, CancellationToken cancellationToken)
    {
        var queryable = userManager.Users.AsNoTracking();

        // Role filtering — join IdentityUserRoles → Roles on the current
        // AppRole names so stale assignments (deleted roles) are filtered out.
        var currentRoles = Enum.GetNames<Authorization.Enums.AppRole>().ToList();
        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            var role = query.Role.Trim();
            queryable = queryable.Where(u => db.UserRoles
                .Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.Name == role)));
        }
        else if (query.CurrentRolesOnly is not false)
        {
            queryable = queryable.Where(u => db.UserRoles
                .Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.Name != null && currentRoles.Contains(r.Name))));
        }

        // Search
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            queryable = queryable.Where(u =>
                (u.Email != null && u.Email.Contains(search)) ||
                (u.UserName != null && u.UserName.Contains(search)) ||
                (u.FirstName != null && u.FirstName.Contains(search)) ||
                (u.LastName != null && u.LastName.Contains(search)) ||
                ((u.FirstName + " " + u.LastName).Contains(search)));
        }

        // Sorting
        queryable = query.SortOrder == SortOrder.Desc
            ? query.SortColumn switch
            {
                UserSortColumn.FirstName => queryable.OrderByDescending(u => u.FirstName),
                UserSortColumn.LastName => queryable.OrderByDescending(u => u.LastName),
                UserSortColumn.Email => queryable.OrderByDescending(u => u.Email),
                UserSortColumn.FullName => queryable.OrderByDescending(u => u.FirstName).ThenByDescending(u => u.LastName),
                _ => queryable.OrderByDescending(u => u.UserName)
            }
            : query.SortColumn switch
            {
                UserSortColumn.FirstName => queryable.OrderBy(u => u.FirstName),
                UserSortColumn.LastName => queryable.OrderBy(u => u.LastName),
                UserSortColumn.Email => queryable.OrderBy(u => u.Email),
                UserSortColumn.FullName => queryable.OrderBy(u => u.FirstName).ThenBy(u => u.LastName),
                _ => queryable.OrderBy(u => u.UserName)
            };

        // Page first, then hydrate roles — Identity roles can't be projected in one SQL query
        var pagedUsers = await PagedList<User>.CreateAsync(queryable, query.Page ?? 1, query.PageSize ?? 15);

        var userResponses = new List<UserResponse>();
        foreach (var u in pagedUsers)
        {
            var roles = await userManager.GetRolesAsync(u);
            userResponses.Add(new UserResponse(u.Id, u.UserName, u.Email, u.FirstName, u.LastName, [.. roles], [], u.LockoutEnd is null || u.LockoutEnd <= DateTimeOffset.UtcNow, u.AssignedLgas));
        }

        var meta = new PaginationMeta
        {
            CurrentPage = pagedUsers.CurrentPage,
            From = pagedUsers.TotalCount == 0 ? null : (pagedUsers.CurrentPage - 1) * pagedUsers.PageSize + 1,
            LastPage = pagedUsers.TotalPages,
            Path = "/api/v1/users",
            PerPage = pagedUsers.PageSize,
            To = pagedUsers.TotalCount == 0 ? null : (pagedUsers.CurrentPage - 1) * pagedUsers.PageSize + userResponses.Count,
            Total = pagedUsers.TotalCount
        };

        var links = new PaginationLinks
        {
            First = $"?page=1&per_page={pagedUsers.PageSize}&search={query.Search}&sort_column={query.SortColumn}&sort_order={query.SortOrder}",
            Last = $"?page={pagedUsers.TotalPages}&per_page={pagedUsers.PageSize}&search={query.Search}&sort_column={query.SortColumn}&sort_order={query.SortOrder}",
            Prev = pagedUsers.HasPrevious ? $"?page={pagedUsers.CurrentPage - 1}&per_page={pagedUsers.PageSize}&search={query.Search}&sort_column={query.SortColumn}&sort_order={query.SortOrder}" : null,
            Next = pagedUsers.HasNext ? $"?page={pagedUsers.CurrentPage + 1}&per_page={pagedUsers.PageSize}&search={query.Search}&sort_column={query.SortColumn}&sort_order={query.SortOrder}" : null
        };

        return new PaginatedResponse<UserResponse>(userResponses, meta, links);
    }
}
