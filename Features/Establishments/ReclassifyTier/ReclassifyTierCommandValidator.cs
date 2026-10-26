namespace LoungeClubsApi.Features.Establishments.ReclassifyTier;

public class ReclassifyTierCommandValidator : AbstractValidator<ReclassifyTierCommand>
{
    public ReclassifyTierCommandValidator()
    {
        RuleFor(x => x.TierId).NotEmpty().WithMessage("A license tier is required.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000)
            .WithMessage("A reason for the reclassification is required.");
    }
}
