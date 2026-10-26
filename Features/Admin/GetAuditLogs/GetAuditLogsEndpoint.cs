using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Models.Shared;

namespace LoungeClubsApi.Features.Admin.GetAuditLogs;

public class GetAuditLogsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("admin/audit-logs", async ([AsParameters] GetAuditLogsQuery query, IMediator mediator, CancellationToken ct) =>
        {
            var response = await mediator.SendQueryAsync<GetAuditLogsQuery, PaginatedResponse<AuditLogResponse>>(query, ct);
            return Results.Ok(response);
        })
        .HasApiVersion(1)
        .WithName(nameof(GetAuditLogsEndpoint))
        .WithTags("Admin")
        .WithSummary("List audit logs")
        .WithDescription("Retrieves a paginated list of audit log entries ordered by most recent first.")
        .Produces<PaginatedResponse<AuditLogResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .HasPermissions(AppPermission.AuditView);
    }
}
