namespace LoungeClubsApi.Features.Notifications.MarkAllNotificationsRead;

public class MarkAllNotificationsReadEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("notifications/read-all", async (IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                await mediator.SendCommandAsync(new MarkAllNotificationsReadCommand(), ct);
                return Results.Ok(new { Message = "All notifications marked as read." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
            }
        })
        .HasApiVersion(1)
        .WithName(nameof(MarkAllNotificationsReadEndpoint))
        .WithTags("Notifications")
        .WithSummary("Mark all of the current user's notifications as read")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .RequireAuthorization();
    }
}
