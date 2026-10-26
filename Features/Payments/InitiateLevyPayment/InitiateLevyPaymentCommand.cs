namespace LoungeClubsApi.Features.Payments.InitiateLevyPayment;

public record InitiateLevyPaymentCommand(
    Guid LevyAssessmentId,
    string? IdempotencyKey,
    string? CallbackUrl
) : ICommand<InitiateLevyPaymentResponse>;
