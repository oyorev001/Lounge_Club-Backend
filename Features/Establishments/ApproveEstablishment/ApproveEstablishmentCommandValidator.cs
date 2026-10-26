namespace LoungeClubsApi.Features.Establishments.ApproveEstablishment;

public class ApproveEstablishmentCommandValidator : AbstractValidator<ApproveEstablishmentCommand>
{
    public ApproveEstablishmentCommandValidator()
    {
        RuleFor(x => x.TierId).NotEmpty().WithMessage("A license tier must be assigned on approval.");
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
