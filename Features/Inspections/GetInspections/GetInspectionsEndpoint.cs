using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Models.Shared;

namespace LoungeClubsApi.Features.Inspections.GetInspections;

public class GetInspectionsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("inspections", async ([AsParameters] GetInspectionsQuery query, IMediator mediator, CancellationToken ct) =>
        {
            var response = await mediator.SendQueryAsync<GetInspectionsQuery, PaginatedResponse<InspectionResponse>>(query, ct);
            return Results.Ok(response);
        })
        .HasApiVersion(1)
        .WithName(nameof(GetInspectionsEndpoint))
        .WithTags("Inspections")
        .WithSummary("List inspections")
        .WithDescription("Retrieves a paginated list of inspections with optional filters.")
        .Produces<PaginatedResponse<InspectionResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .HasPermissions(AppPermission.InspectionsViewAll);
    }
}
