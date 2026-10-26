namespace LoungeClubsApi.Features.Levies.GenerateAnnualLevies;

public class GenerateAnnualLeviesCommandValidator : AbstractValidator<GenerateAnnualLeviesCommand>
{
    public GenerateAnnualLeviesCommandValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(DateTime.UtcNow.Year, DateTime.UtcNow.Year + 1);
    }
}
