using LoungeClubsApi.Models.Shared;

namespace LoungeClubsApi.Features.Notifications.GetMyNotifications;

public record MyNotificationsResponse(
    PaginatedResponse<NotificationResponse> Items,
    int UnreadCount);
