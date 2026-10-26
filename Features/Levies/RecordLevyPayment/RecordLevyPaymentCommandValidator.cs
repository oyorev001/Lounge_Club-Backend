namespace LoungeClubsApi.Features.Levies.RecordLevyPayment;

public class RecordLevyPaymentCommandValidator : AbstractValidator<RecordLevyPaymentCommand>
{
    public RecordLevyPaymentCommandValidator()
    {
        RuleFor(x => x.LevyId).NotEmpty();
        RuleFor(x => x.Note).MaximumLength(500);
    }
}
