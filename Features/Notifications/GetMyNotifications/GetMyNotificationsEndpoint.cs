namespace LoungeClubsApi.Features.Notifications.GetMyNotifications;

public class GetMyNotificationsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("notifications", async ([AsParameters] GetMyNotificationsQuery query, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var response = await mediator.SendQueryAsync<GetMyNotificationsQuery, MyNotificationsResponse>(query, ct);
                return Results.Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
            }
        })
        .HasApiVersion(1)
        .WithName(nameof(GetMyNotificationsEndpoint))
        .WithTags("Notifications")
        .WithSummary("List the current user's notifications")
        .Produces<MyNotificationsResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .RequireAuthorization();
    }
}
