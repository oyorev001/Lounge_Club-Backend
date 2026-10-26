using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Inspections.LogInspection;

public class LogInspectionEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("inspections/{id:guid}/complete", async (Guid id, [FromBody] LogInspectionRequest body, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var result = await mediator.SendCommandAsync<LogInspectionCommand, LogInspectionResponse>(
                    new LogInspectionCommand(id, body.Outcome, body.Notes, body.EvidenceUrls, body.Latitude, body.Longitude), ct);
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
        .WithName(nameof(LogInspectionEndpoint))
        .WithTags("Inspections")
        .WithSummary("Log the outcome of a scheduled inspection")
        .Produces<LogInspectionResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .HasPermissions(AppPermission.InspectionsConduct);
    }
}
