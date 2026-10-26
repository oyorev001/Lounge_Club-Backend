using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Establishments.ReinstateEstablishment;

public class ReinstateEstablishmentEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("establishments/{id:guid}/reinstate", async (Guid id, [FromBody] ReinstateEstablishmentCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.SendCommandAsync<ReinstateEstablishmentCommand, ReinstateEstablishmentResponse>(command with { EstablishmentId = id }, ct);
            return Results.Ok(result);
        })
        .HasApiVersion(1)
        .WithName(nameof(ReinstateEstablishmentEndpoint))
        .WithTags("Establishments")
        .WithSummary("Reinstate a suspended establishment")
        .WithDescription("Lifts a suspension: the venue returns to Approved. Certificates revoked at suspension are NOT re-issued — the owner must settle a new levy for a fresh certificate.")
        .Produces<ReinstateEstablishmentResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .HasPermissions(AppPermission.EstablishmentsSuspend);
    }
}
