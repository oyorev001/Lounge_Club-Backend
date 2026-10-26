using LoungeClubsApi.Data;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Interfaces;
using LoungeClubsApi.Models.Shared;

namespace LoungeClubsApi.Features.Notifications.GetMyNotifications;

public class GetMyNotificationsQueryHandler(
    AppDbContext db,
    ICurrentUserService currentUser)
    : IQueryHandler<GetMyNotificationsQuery, MyNotificationsResponse>
{
    public async Task<MyNotificationsResponse> Handle(GetMyNotificationsQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("You must be logged in.");

        var unreadCount = await db.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

        var projected = db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationResponse(
                n.Id,
                n.Type,
                n.Title,
                n.Message,
                n.Link,
                n.IsRead,
                n.CreatedAt));

        var pagedList = await PagedList<NotificationResponse>.CreateAsync(projected, query.Page ?? 1, query.PageSize ?? 15);

        return new MyNotificationsResponse(
            pagedList.ToPaginatedResponse(items => items, "notifications"),
            unreadCount);
    }
}
