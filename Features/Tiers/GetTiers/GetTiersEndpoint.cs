using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Tiers.GetTiers;

public class GetTiersEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("tiers", async (IMediator mediator, CancellationToken ct) =>
        {
            var response = await mediator.SendQueryAsync<GetTiersQuery, List<TierResponse>>(new GetTiersQuery(), ct);
            return Results.Ok(response);
        })
        .HasApiVersion(1)
        .WithName(nameof(GetTiersEndpoint))
        .WithTags("Tiers")
        .WithSummary("Get active license tiers")
        .WithDescription("Retrieves all active license tiers ordered by sort order.")
        .Produces<List<TierResponse>>(StatusCodes.Status200OK)
        .AllowAnonymous().RequireRateLimiting("public-read");
    }
}
