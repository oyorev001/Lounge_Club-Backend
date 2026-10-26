using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Models.Shared;

namespace LoungeClubsApi.Features.Fines.GetFines;

public class GetFinesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("fines", async ([AsParameters] GetFinesQuery query, IMediator mediator, CancellationToken ct) =>
        {
            var response = await mediator.SendQueryAsync<GetFinesQuery, PaginatedResponse<FineResponse>>(query, ct);
            return Results.Ok(response);
        })
        .HasApiVersion(1)
        .WithName(nameof(GetFinesEndpoint))
        .WithTags("Fines")
        .WithSummary("List fines")
        .WithDescription("Retrieves a paginated list of fines with optional filters.")
        .Produces<PaginatedResponse<FineResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .HasPermissions(AppPermission.FinesViewAll);
    }
}
