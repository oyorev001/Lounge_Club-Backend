using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Notifications.GetMyNotifications;

public record NotificationResponse(
    Guid Id,
    NotificationType Type,
    string Title,
    string Message,
    string? Link,
    bool IsRead,
    DateTimeOffset CreatedAt);
