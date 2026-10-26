using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Interfaces;

public interface IPaymentGateway
{
    /// <summary>Initializes a hosted-checkout payment; returns the URL to send the payer to.</summary>
    Task<PaymentInitResult?> InitializePaymentAsync(PaymentInitRequest request, CancellationToken cancellationToken = default);

    /// <summary>Server-side verification of a transaction by our reference — use on webhooks and status polls.</summary>
    Task<PaymentVerifyResult?> VerifyPaymentAsync(string reference, CancellationToken cancellationToken = default);

    /// <summary>Verifies a webhook signature over the raw payload.</summary>
    bool VerifyWebhookSignature(string payload, string signature);
}

public record PaymentInitRequest(
    string Email,
    string FirstName,
    string LastName,
    string Phone,
    decimal AmountNaira,
    string Reference,
    TransactionContext Context,
    Guid ContextId,
    Guid? EstablishmentId,
    string? CallbackUrl);

public record PaymentInitResult(
    string AuthorizationUrl,
    string AccessCode,
    string ProcessorReference);

public record PaymentVerifyResult(
    bool Succeeded,
    long AmountKobo,
    string? Channel,
    string? GatewayReference,
    DateTimeOffset? PaidAt);
