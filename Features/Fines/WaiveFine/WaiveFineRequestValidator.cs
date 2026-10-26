namespace LoungeClubsApi.Features.Fines.WaiveFine;

public class WaiveFineRequestValidator : AbstractValidator<WaiveFineRequest>
{
    public WaiveFineRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MinimumLength(10).MaximumLength(1000);
    }
}
