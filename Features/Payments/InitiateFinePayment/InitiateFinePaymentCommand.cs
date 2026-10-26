namespace LoungeClubsApi.Features.Payments.InitiateFinePayment;

public record InitiateFinePaymentCommand(
    Guid FineId,
    string? IdempotencyKey,
    string? CallbackUrl
) : ICommand<InitiateFinePaymentResponse>;
