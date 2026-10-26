using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Admin.GetDuplicatePayments;

/// <summary>
/// Reconciliation view: successful transactions where an EARLIER successful
/// transaction already covered the same debt — i.e. the money collected twice.
/// The earliest successful settlement is the legitimate one; the rest are
/// refund candidates.
/// </summary>
public class GetDuplicatePaymentsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("admin/payments/duplicates", async (AppDbContext db, CancellationToken ct) =>
        {
            // Debts covered by more than one successful transaction.
            var dupKeys = await db.Transactions
                .Where(t => t.Status == PaymentStatus.Successful && t.ContextId != null)
                .GroupBy(t => new { t.Context, t.ContextId })
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToListAsync(ct);

            var items = new List<object>();
            foreach (var key in dupKeys)
            {
                var txns = await db.Transactions
                    .Where(t => t.Status == PaymentStatus.Successful
                        && t.Context == key.Context && t.ContextId == key.ContextId)
                    .OrderBy(t => t.PaidAt)
                    .ToListAsync(ct);

                // First = the legitimate settlement; the rest are duplicates.
                foreach (var t in txns.Skip(1))
                {
                    var (estName, debtRef) = key.Context == TransactionContext.Levy
                        ? await db.LevyAssessments
                            .Where(l => l.Id == t.ContextId)
                            .Select(l => new ValueTuple<string, string>(l.Establishment.Name, l.Reference))
                            .FirstOrDefaultAsync(ct)
                        : await db.Fines
                            .Where(f => f.Id == t.ContextId)
                            .Select(f => new ValueTuple<string, string>(f.Establishment.Name, f.Reference))
                            .FirstOrDefaultAsync(ct);

                    items.Add(new
                    {
                        t.Id,
                        t.Reference,
                        t.Amount,
                        t.PaidAt,
                        context = key.Context.ToString(),
                        debtReference = debtRef,
                        establishmentName = estName,
                        flagged = t.Narration != null && t.Narration.Contains("refund candidate"),
                        t.Narration,
                        t.SenderName,
                    });
                }
            }

            return Results.Ok(items.OrderByDescending(i => ((dynamic)i).PaidAt));
        })
        .HasApiVersion(1)
        .WithName(nameof(GetDuplicatePaymentsEndpoint))
        .WithTags("Admin")
        .WithSummary("List duplicate settlements that are refund candidates")
        .RequireAuthorization()
        .HasAnyRole(AppRole.Admin);
    }
}
