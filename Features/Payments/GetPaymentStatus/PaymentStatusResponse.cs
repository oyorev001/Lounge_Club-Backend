using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Payments.GetPaymentStatus;

public record PaymentStatusResponse(
    string Reference,
    PaymentStatus Status,
    decimal Amount,
    TransactionContext Context,
    DateTimeOffset? PaidAt,
    Guid? CertificateId,
    string? CertificateNumber,
    string? FailureReason);
