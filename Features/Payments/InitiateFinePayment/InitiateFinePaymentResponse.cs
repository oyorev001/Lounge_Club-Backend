using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Payments.InitiateFinePayment;

public record InitiateFinePaymentResponse(
    Guid TransactionId,
    string Reference,
    decimal Amount,
    PaymentStatus Status,
    string? CheckoutUrl,
    DateTimeOffset? ExpiresAt);
