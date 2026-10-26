namespace LoungeClubsApi.Features.Payments.InitiateLevyPayment;

public class InitiateLevyPaymentCommandValidator : AbstractValidator<InitiateLevyPaymentCommand>
{
    public InitiateLevyPaymentCommandValidator()
    {
        RuleFor(x => x.LevyAssessmentId).NotEmpty();
        RuleFor(x => x.IdempotencyKey).MaximumLength(100);
    }
}
