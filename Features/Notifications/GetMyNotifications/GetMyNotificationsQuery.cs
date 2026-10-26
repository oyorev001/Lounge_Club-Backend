namespace LoungeClubsApi.Features.Notifications.GetMyNotifications;

public record GetMyNotificationsQuery(
    [property: FromQuery(Name = "page")] int? Page,
    [property: FromQuery(Name = "per_page")] int? PageSize
) : IQuery<MyNotificationsResponse>;
