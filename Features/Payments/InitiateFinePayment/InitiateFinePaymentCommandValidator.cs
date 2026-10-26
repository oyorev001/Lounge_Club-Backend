namespace LoungeClubsApi.Features.Payments.InitiateFinePayment;

public class InitiateFinePaymentCommandValidator : AbstractValidator<InitiateFinePaymentCommand>
{
    public InitiateFinePaymentCommandValidator()
    {
        RuleFor(x => x.FineId).NotEmpty();
        RuleFor(x => x.IdempotencyKey).MaximumLength(100);
    }
}
