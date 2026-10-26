using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Features.Payments.RejectManualPayment;

/// <summary>
/// Reject a manual payment awaiting confirmation — releases the record so the
/// debt can be re-recorded or settled another way. Without this, an
/// AwaitingConfirmation record is a one-way trap.
/// </summary>
public class RejectManualPaymentEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("payments/{id:guid}/reject", async (
            Guid id,
            [FromBody] RejectRequest request,
            AppDbContext db,
            ICurrentUserService currentUser,
            CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            if (userId is null)
                return Results.Unauthorized();

            var transaction = await db.Transactions.FirstOrDefaultAsync(t => t.Id == id, ct);
            if (transaction is null)
                return Results.NotFound(new { error = "Transaction not found." });
            if (transaction.Status != PaymentStatus.AwaitingConfirmation)
                return Results.BadRequest(new { error = $"Transaction is not awaiting confirmation (status: {transaction.Status})." });
            if (transaction.Channel != PaymentChannel.Manual)
                return Results.BadRequest(new { error = "Only manual payments can be rejected." });

            // Two-person rule applies to rejection too — the recorder can't
            // silently delete their own record.
            if (transaction.RecordedById == userId)
                return Results.Json(
                    new { error = "The staff member who recorded this payment cannot reject it — a second authorized officer must." },
                    statusCode: StatusCodes.Status403Forbidden);

            transaction.Status = PaymentStatus.Cancelled;
            transaction.FailureReason = $"Manual record rejected: {(request.Reason ?? "no reason given").Trim()}";
            await db.SaveChangesAsync(ct);

            return Results.Ok(new { transaction.Id, transaction.Status });
        })
        .HasApiVersion(1)
        .WithName(nameof(RejectManualPaymentEndpoint))
        .WithTags("Payments")
        .RequireAuthorization()
        .HasAnyRole(AppRole.Admin);
    }

    private record RejectRequest(string? Reason);
}
