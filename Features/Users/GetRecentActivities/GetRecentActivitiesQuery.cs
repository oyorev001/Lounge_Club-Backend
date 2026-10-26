

namespace LoungeClubsApi.Features.Users.GetRecentActivities;

public record GetRecentActivitiesQuery(string? UserId) : IQuery<List<RecentActivityResponse>>;
