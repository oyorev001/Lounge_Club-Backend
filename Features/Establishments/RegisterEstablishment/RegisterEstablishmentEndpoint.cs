using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Establishments.RegisterEstablishment;

public class RegisterEstablishmentEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("establishments", async ([FromBody] RegisterEstablishmentCommand command, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var result = await mediator.SendCommandAsync<RegisterEstablishmentCommand, RegisterEstablishmentResponse>(command, ct);
                return Results.Ok(result);
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
        .WithName(nameof(RegisterEstablishmentEndpoint))
        .WithTags("Establishments")
        .WithSummary("Register a new lounge, bar or club")
        .Produces<RegisterEstablishmentResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .HasRole(AppRole.BusinessOwner, AppRole.Admin);
    }
}
