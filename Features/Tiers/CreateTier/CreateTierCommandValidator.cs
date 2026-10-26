namespace LoungeClubsApi.Features.Tiers.CreateTier;

public class CreateTierCommandValidator : AbstractValidator<CreateTierCommand>
{
    public CreateTierCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.AnnualFee).GreaterThan(0);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinScore).GreaterThanOrEqualTo(0);
    }
}
