namespace LoungeClubsApi.Features.Establishments.RejectEstablishment;

public class RejectEstablishmentCommandValidator : AbstractValidator<RejectEstablishmentCommand>
{
    public RejectEstablishmentCommandValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MinimumLength(10).MaximumLength(1000)
            .WithMessage("A rejection reason of at least 10 characters is required.");
    }
}
