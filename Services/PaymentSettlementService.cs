using LoungeClubsApi.Attributes;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities;
using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Services;

/// <summary>
/// Single place that turns a confirmed payment into a settled transaction and
/// triggers levy/fine settlement (certificate issuance). Used by the Paystack
/// webhook and by the status endpoint's verify-on-poll fallback.
/// </summary>
[ScopedService]
public class PaymentSettlementService(AppDbContext db, CertificateIssuanceService certificateIssuance)
{
    public async Task SettleSuccessfulAsync(
        Transaction transaction,
        string? gatewayReference,
        string? channel,
        string? payerName,
        DateTimeOffset? paidAt,
        CancellationToken cancellationToken)
    {
        // Atomically claim settlement: only one caller (webhook OR status poll
        // OR manual reconcile) transitions the row out of Pending/Failed —
        // concurrent deliveries can't double-settle.
        var settledAt = paidAt ?? DateTimeOffset.UtcNow;
        var claimed = await db.Transactions
            .Where(t => t.Id == transaction.Id
                && (t.Status == PaymentStatus.Pending
                    || t.Status == PaymentStatus.Failed
                    || t.Status == PaymentStatus.Cancelled))
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.Status, PaymentStatus.Successful)
                .SetProperty(t => t.PaidAt, settledAt)
                .SetProperty(t => t.GatewayReference, t => gatewayReference ?? t.GatewayReference)
                .SetProperty(t => t.SenderName, t => payerName ?? t.SenderName),
                cancellationToken);

        if (claimed == 0)
            return; // already settled by a concurrent caller — idempotent

        // Sync the tracked entity and persist channel/narration details.
        transaction.Status = PaymentStatus.Successful;
        transaction.PaidAt = settledAt;
        transaction.GatewayReference = gatewayReference ?? transaction.GatewayReference;
        transaction.SenderName = payerName ?? transaction.SenderName;
        if (!string.IsNullOrWhiteSpace(channel))
        {
            transaction.Narration = $"Paystack — {channel}";
            transaction.Channel = channel.Equals("card", StringComparison.OrdinalIgnoreCase)
                ? PaymentChannel.Card
                : PaymentChannel.BankTransfer;
        }

        await db.SaveChangesAsync(cancellationToken);

        if (transaction.Context == TransactionContext.Levy)
            await certificateIssuance.IssueForPaidLevyAsync(transaction, cancellationToken);
        else if (transaction.Context == TransactionContext.Fine)
            await certificateIssuance.MarkFinePaidAsync(transaction, cancellationToken);
    }
}
