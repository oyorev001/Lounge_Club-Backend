namespace LoungeClubsApi.Features.Notifications.MarkNotificationRead;

public class MarkNotificationReadEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("notifications/{id:guid}/read", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                await mediator.SendCommandAsync(new MarkNotificationReadCommand(id), ct);
                return Results.Ok(new { Message = "Notification marked as read." });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
            }
        })
        .HasApiVersion(1)
        .WithName(nameof(MarkNotificationReadEndpoint))
        .WithTags("Notifications")
        .WithSummary("Mark a notification as read")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization();
    }
}
