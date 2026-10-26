using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Models.Shared;

namespace LoungeClubsApi.Features.Admin.GetDefaulters;

public class GetDefaultersEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("admin/defaulters", async ([AsParameters] GetDefaultersQuery query, IMediator mediator, CancellationToken ct) =>
        {
            var response = await mediator.SendQueryAsync<GetDefaultersQuery, PaginatedResponse<DefaulterResponse>>(query, ct);
            return Results.Ok(response);
        })
        .HasApiVersion(1)
        .WithName(nameof(GetDefaultersEndpoint))
        .WithTags("Admin")
        .WithSummary("List defaulting establishments")
        .WithDescription("Approved establishments with no active certificate or with overdue levies.")
        .Produces<PaginatedResponse<DefaulterResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .HasPermissions(AppPermission.ReportsView);
    }
}
