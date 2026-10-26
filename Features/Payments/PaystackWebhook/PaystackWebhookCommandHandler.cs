using System.Text.Json;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Interfaces;
using LoungeClubsApi.Services;

namespace LoungeClubsApi.Features.Payments.PaystackWebhook;

public class PaystackWebhookCommandHandler(
    AppDbContext db,
    IPaymentGateway paymentGateway,
    PaymentSettlementService settlement,
    ILogger<PaystackWebhookCommandHandler> logger)
    : ICommandHandler<PaystackWebhookCommand, PaystackWebhookResponse>
{
    public async Task<PaystackWebhookResponse> Handle(PaystackWebhookCommand request, CancellationToken cancellationToken)
    {
        var log = new WebhookLog
        {
            Provider = "Paystack",
            Payload = request.Payload,
            Signature = request.Signature
        };

        try
        {
            log.SignatureValid = !string.IsNullOrWhiteSpace(request.Signature)
                && paymentGateway.VerifyWebhookSignature(request.Payload, request.Signature);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Paystack webhook signature verification threw");
            log.SignatureValid = false;
        }

        if (!log.SignatureValid)
        {
            log.ProcessingResult = "invalid signature";
            db.WebhookLogs.Add(log);
            await db.SaveChangesAsync(cancellationToken);
            return new PaystackWebhookResponse(false);
        }

        try
        {
            using var doc = JsonDocument.Parse(request.Payload);
            var root = doc.RootElement;

            var eventType = GetString(root, "event");
            log.EventType = eventType;

            var data = GetObject(root, "data");
            var dataId = GetString(data, "id");
            log.EventId = dataId is not null ? $"{eventType}:{dataId}" : null;

            // Insert-first dedup: Paystack retries on non-200s, and two deliveries
            // of the same event must not both reach settlement.
            db.WebhookLogs.Add(log);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (ex.IsUniqueViolation())
            {
                // A prior delivery crashed before finishing leaves a dedup row
                // with Processed=false — reprocess it instead of dropping the
                // event permanently.
                db.Entry(log).State = EntityState.Detached;
                var existing = await db.WebhookLogs
                    .FirstOrDefaultAsync(w => w.Provider == "Paystack" && w.EventId == log.EventId, cancellationToken);
                if (existing is null || existing.Processed)
                {
                    logger.LogInformation("Duplicate Paystack webhook {EventId} ignored", log.EventId);
                    return new PaystackWebhookResponse(true);
                }
                log = existing;
            }

            var reference = GetString(data, "reference");

            if (!string.Equals(eventType, "charge.success", StringComparison.OrdinalIgnoreCase))
            {
                log.Processed = true;
                log.ProcessingResult = $"ignored event '{eventType ?? "unknown"}'";
                await db.SaveChangesAsync(cancellationToken);
                return new PaystackWebhookResponse(true);
            }

            if (string.IsNullOrWhiteSpace(reference))
            {
                log.Processed = true;
                log.ProcessingResult = "no reference in payload";
                await db.SaveChangesAsync(cancellationToken);
                return new PaystackWebhookResponse(true);
            }

            // Paystack carries our transaction reference — match Pending, Failed
            // or Cancelled. A cancelled checkout can still be genuinely paid:
            // staff may record a manual payment while the card payment is in
            // flight — the webhook must still settle the real money received.
            var transaction = await db.Transactions
                .FirstOrDefaultAsync(t => t.Reference == reference
                    && (t.Status == PaymentStatus.Pending
                        || t.Status == PaymentStatus.Failed
                        || t.Status == PaymentStatus.Cancelled),
                    cancellationToken);

            if (transaction is null)
            {
                log.Processed = true;
                log.ProcessingResult = $"unmatched or already settled ({reference})";
                await db.SaveChangesAsync(cancellationToken);
                return new PaystackWebhookResponse(true);
            }

            // Amounts arrive in kobo — a missing amount must never settle, and
            // an underpayment must not silently stay pending forever.
            var paidKobo = GetLong(data, "amount");
            var expectedKobo = (long)Math.Round(transaction.Amount * 100m, MidpointRounding.AwayFromZero);
            if (paidKobo is null)
            {
                log.Processed = true;
                log.ProcessingResult = $"matched {transaction.Reference} but payload carried no amount — rejected";
                await db.SaveChangesAsync(cancellationToken);
                return new PaystackWebhookResponse(true);
            }

            var currency = GetString(data, "currency");
            if (!string.Equals(currency, "NGN", StringComparison.OrdinalIgnoreCase))
            {
                log.Processed = true;
                log.ProcessingResult = $"matched {transaction.Reference} but currency '{currency}' is not NGN — marked failed";
                transaction.Status = PaymentStatus.Failed;
                transaction.FailureReason = $"Payment received in {currency ?? "unknown currency"} — contact support.";
                await db.SaveChangesAsync(cancellationToken);
                return new PaystackWebhookResponse(true);
            }

            if (paidKobo.Value < expectedKobo)
            {
                // Mark Failed so the filtered pending-checkout index frees —
                // otherwise the levy is locked for up to 24h until the timeout
                // job expires it and the owner can't retry with the right amount.
                log.Processed = true;
                log.ProcessingResult = $"matched {transaction.Reference} but amount {paidKobo.Value / 100m:N2} < expected {transaction.Amount:N2} — marked failed";
                transaction.Status = PaymentStatus.Failed;
                transaction.FailureReason = $"Amount paid ₦{paidKobo.Value / 100m:N2} is less than the required ₦{transaction.Amount:N2}.";
                await db.SaveChangesAsync(cancellationToken);
                return new PaystackWebhookResponse(true);
            }

            var channel = GetString(data, "channel");
            var gatewayResponse = GetString(data, "gateway_response");
            var customer = GetObject(data, "customer");
            var payerName = customer.ValueKind == JsonValueKind.Object
                ? $"{GetString(customer, "first_name")} {GetString(customer, "last_name")}".Trim()
                : null;
            var paidAt = DateTimeOffset.TryParse(GetString(data, "paid_at"), out var parsed) ? parsed : (DateTimeOffset?)null;

            await settlement.SettleSuccessfulAsync(
                transaction,
                GetString(data, "id") ?? gatewayResponse,
                channel,
                string.IsNullOrWhiteSpace(payerName) ? null : payerName,
                paidAt,
                cancellationToken);

            transaction.WebhookId = GetString(data, "id");
            await db.SaveChangesAsync(cancellationToken);

            log.Processed = true;
            log.ProcessingResult = $"matched {transaction.Reference}; payment marked successful";
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Never let a processing bug make Paystack retry-loop — record the failure and return Ok.
            logger.LogError(ex, "Failed to process Paystack webhook");
            try
            {
                // The scoped context may carry partially-settled entities
                // (cert, outbox, levy=Paid) — detach them so logging the
                // failure can't commit the settlement it just failed on.
                foreach (var entry in db.ChangeTracker.Entries()
                    .Where(e => e.State != EntityState.Unchanged
                        && e.Entity is not WebhookLog)
                    .ToList())
                {
                    entry.State = EntityState.Detached;
                }
                log.Processed = false;
                log.ProcessingResult = $"error: {ex.Message}";
                await db.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception saveEx)
            {
                logger.LogError(saveEx, "Failed to persist Paystack webhook log");
            }
        }

        return new PaystackWebhookResponse(true);
    }

    private static JsonElement GetObject(JsonElement element, string name)
    {
        if (element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(name, out var prop)
            && prop.ValueKind == JsonValueKind.Object)
        {
            return prop;
        }

        return default;
    }

    private static long? GetLong(JsonElement element, params string[] names)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var prop)
                && prop.ValueKind == JsonValueKind.Number
                && prop.TryGetInt64(out var value))
            {
                return value;
            }
        }

        return null;
    }

    private static string? GetString(JsonElement element, params string[] names)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.String)
                    return prop.GetString();
                if (prop.ValueKind == JsonValueKind.Number)
                    return prop.GetRawText();
            }
        }

        return null;
    }
}
