using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Features.Payments.GetPendingConfirmations;

/// <summary>Manual payments awaiting second-person confirmation.</summary>
public class GetPendingConfirmationsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("payments/pending-confirmation", async (AppDbContext db, CancellationToken ct) =>
        {
            var items = await db.Transactions
                .AsNoTracking()
                .Where(t => t.Status == PaymentStatus.AwaitingConfirmation)
                .OrderBy(t => t.CreatedAt)
                .Select(t => new
                {
                    t.Id,
                    t.Reference,
                    t.Amount,
                    t.Context,
                    t.ContextId,
                    t.ExternalReference,
                    t.Narration,
                    t.RecordedById,
                    EstablishmentName = t.Establishment != null ? t.Establishment.Name : null,
                    t.CreatedAt
                })
                .ToListAsync(ct);
            return Results.Ok(items);
        })
        .HasApiVersion(1)
        .WithName(nameof(GetPendingConfirmationsEndpoint))
        .WithTags("Payments")
        .RequireAuthorization()
        .HasAnyRole(AppRole.Admin);
    }
}
