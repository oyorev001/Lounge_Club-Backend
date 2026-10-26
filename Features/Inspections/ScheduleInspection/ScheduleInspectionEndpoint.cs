using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Inspections.ScheduleInspection;

public class ScheduleInspectionEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("inspections", async ([FromBody] ScheduleInspectionCommand command, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var result = await mediator.SendCommandAsync<ScheduleInspectionCommand, ScheduleInspectionResponse>(command, ct);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .HasApiVersion(1)
        .WithName(nameof(ScheduleInspectionEndpoint))
        .WithTags("Inspections")
        .WithSummary("Schedule an establishment inspection")
        .Produces<ScheduleInspectionResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .HasPermissions(AppPermission.InspectionsConduct);
    }
}
