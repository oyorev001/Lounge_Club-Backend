using Microsoft.EntityFrameworkCore;
using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Data;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Features.Users.GetRecentActivities;

public class GetRecentActivitiesHandler(
    AppDbContext context,
    ICurrentUserService currentUserService,
    IUserPermissionService permissions
) : IQueryHandler<GetRecentActivitiesQuery, List<RecentActivityResponse>>
{
    public async Task<List<RecentActivityResponse>> Handle(GetRecentActivitiesQuery request, CancellationToken cancellationToken)
    {
        var targetUserId = request.UserId;

        // If no UserId provided, use current user
        if (string.IsNullOrEmpty(targetUserId))
        {
            targetUserId = currentUserService.UserId;
        }

        if (string.IsNullOrEmpty(targetUserId))
        {
            return [];
        }

        // Authorization Check
        // If requesting another user's data, the caller needs Users.ViewActivity.
        // Permission claims live on roles in the DB, not on the JWT — resolve
        // them here rather than reading claims off the principal.
        if (targetUserId != currentUserService.UserId
            && !await permissions.HasPermissionAsync(
                currentUserService.UserId!,
                AppPermission.UsersViewActivity.GetDescription(),
                cancellationToken))
        {
            return [];
        }

        var activities = await context.AuditLogs
            .AsNoTracking()
            .Where(x => x.UserId == targetUserId)
            .OrderByDescending(x => x.Timestamp)
            .Take(50)
            .Select(x => new RecentActivityResponse(
                x.Type,
                x.TableName,
                x.OldValues,
                x.NewValues,
                x.IpAddress,
                x.UserAgent,
                x.Timestamp
            ))
            .ToListAsync(cancellationToken);

        return activities;
    }
}
