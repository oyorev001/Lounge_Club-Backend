namespace LoungeClubsApi.Features.Levies.WaiveLevy;

public class WaiveLevyCommandValidator : AbstractValidator<WaiveLevyCommand>
{
    public WaiveLevyCommandValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MinimumLength(10).MaximumLength(500);
    }
}
