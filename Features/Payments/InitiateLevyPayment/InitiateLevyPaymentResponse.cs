using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Payments.InitiateLevyPayment;

public record InitiateLevyPaymentResponse(
    Guid TransactionId,
    string Reference,
    decimal Amount,
    PaymentStatus Status,
    string? CheckoutUrl,
    DateTimeOffset? ExpiresAt);
