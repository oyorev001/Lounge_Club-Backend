using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Admin.GetDashboardStats;

public class GetDashboardStatsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("admin/dashboard", async (IMediator mediator, CancellationToken ct) =>
        {
            var response = await mediator.SendQueryAsync<GetDashboardStatsQuery, DashboardStatsResponse>(new GetDashboardStatsQuery(), ct);
            return Results.Ok(response);
        })
        .HasApiVersion(1)
        .WithName(nameof(GetDashboardStatsEndpoint))
        .WithTags("Admin")
        .WithSummary("Get compliance dashboard statistics")
        .Produces<DashboardStatsResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .HasPermissions(AppPermission.ReportsView);
    }
}
