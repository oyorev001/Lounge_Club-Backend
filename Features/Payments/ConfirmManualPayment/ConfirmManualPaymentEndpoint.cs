using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Interfaces;
using LoungeClubsApi.Services;

namespace LoungeClubsApi.Features.Payments.ConfirmManualPayment;

/// <summary>
/// Two-person rule: confirm a manual payment recorded by another staff member.
/// On confirmation the levy settles (cert issues) or the fine marks paid.
/// </summary>
public class ConfirmManualPaymentEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("payments/{id:guid}/confirm", async (
            Guid id,
            AppDbContext db,
            ICurrentUserService currentUser,
            CertificateIssuanceService certificateIssuance,
            NotificationService notificationService,
            CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            if (userId is null)
                return Results.Unauthorized();

            var transaction = await db.Transactions
                .Include(t => t.Establishment)
                .FirstOrDefaultAsync(t => t.Id == id, ct);
            if (transaction is null)
                return Results.NotFound(new { error = "Transaction not found." });
            if (transaction.Status != PaymentStatus.AwaitingConfirmation)
                return Results.BadRequest(new { error = $"Transaction is not awaiting confirmation (status: {transaction.Status})." });

            // The recorder must not confirm their own record.
            if (transaction.RecordedById == userId)
                return Results.Json(new { error = "The staff member who recorded this payment cannot confirm it — a second authorized officer must confirm." },
                    statusCode: StatusCodes.Status403Forbidden);

            // The underlying debt must still be unsettled — otherwise we'd mark
            // phantom revenue (e.g. the levy was paid via Paystack in the meantime).
            if (transaction.Context == TransactionContext.Levy)
            {
                var levyStatus = await db.LevyAssessments
                    .Where(l => l.Id == transaction.ContextId)
                    .Select(l => l.Status)
                    .FirstOrDefaultAsync(ct);
                if (levyStatus is not (LevyStatus.Pending or LevyStatus.Overdue))
                    return Results.BadRequest(new { error = $"Levy is already {levyStatus} — this record can't be confirmed." });
            }
            else if (transaction.Context == TransactionContext.Fine)
            {
                var fineStatus = await db.Fines
                    .Where(f => f.Id == transaction.ContextId)
                    .Select(f => f.Status)
                    .FirstOrDefaultAsync(ct);
                if (fineStatus is not FineStatus.Unpaid)
                    return Results.BadRequest(new { error = $"Fine is already {fineStatus} — this record can't be confirmed." });
            }

            // Atomic claim — two concurrent confirms can't both flip the row.
            var claimed = await db.Transactions
                .Where(t => t.Id == id && t.Status == PaymentStatus.AwaitingConfirmation)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(t => t.Status, PaymentStatus.Successful)
                    .SetProperty(t => t.ConfirmedById, userId)
                    .SetProperty(t => t.ConfirmedAt, DateTimeOffset.UtcNow), ct);
            if (claimed == 0)
                return Results.BadRequest(new { error = "This payment was already confirmed by someone else." });

            transaction.Status = PaymentStatus.Successful;
            transaction.ConfirmedById = userId;
            transaction.ConfirmedAt = DateTimeOffset.UtcNow;

            if (transaction.Context == TransactionContext.Levy)
            {
                var certificate = await certificateIssuance.IssueForPaidLevyAsync(transaction, ct);
                await db.SaveChangesAsync(ct);
                return Results.Ok(new
                {
                    transaction.Id,
                    transaction.Status,
                    certificateId = certificate?.Id,
                    certificateNumber = certificate?.CertificateNumber
                });
            }

            if (transaction.Context == TransactionContext.Fine)
            {
                await certificateIssuance.MarkFinePaidAsync(transaction, ct);
                await db.SaveChangesAsync(ct);
            }

            return Results.Ok(new { transaction.Id, transaction.Status });
        })
        .HasApiVersion(1)
        .WithName(nameof(ConfirmManualPaymentEndpoint))
        .WithTags("Payments")
        .RequireAuthorization()
        .HasAnyRole(AppRole.Admin);
    }
}
