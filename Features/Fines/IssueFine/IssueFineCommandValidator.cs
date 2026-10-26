namespace LoungeClubsApi.Features.Fines.IssueFine;

public class IssueFineCommandValidator : AbstractValidator<IssueFineCommand>
{
    public IssueFineCommandValidator()
    {
        RuleFor(x => x.EstablishmentId).NotEmpty();
        RuleFor(x => x.FineType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(10_000_000);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
