namespace LoungeClubsApi.Features.Tiers.UpdateTier;

public class UpdateTierCommandValidator : AbstractValidator<UpdateTierCommand>
{
    public UpdateTierCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.AnnualFee).GreaterThan(0);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinScore).GreaterThanOrEqualTo(0);
    }
}
