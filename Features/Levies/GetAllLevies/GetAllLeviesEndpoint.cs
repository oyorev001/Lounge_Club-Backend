using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Models.Shared;

namespace LoungeClubsApi.Features.Levies.GetAllLevies;

public class GetAllLeviesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("levies", async ([AsParameters] GetAllLeviesQuery query, IMediator mediator, CancellationToken ct) =>
        {
            var response = await mediator.SendQueryAsync<GetAllLeviesQuery, PaginatedResponse<LevyListItemResponse>>(query, ct);
            return Results.Ok(response);
        })
        .HasApiVersion(1)
        .WithName(nameof(GetAllLeviesEndpoint))
        .WithTags("Levies")
        .WithSummary("Get all levy assessments")
        .WithDescription("Retrieves a paginated list of levy assessments. Supports filtering by year, status, LGA and establishment name.")
        .Produces<PaginatedResponse<LevyListItemResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .HasPermissions(AppPermission.LeviesViewAll);
    }
}
