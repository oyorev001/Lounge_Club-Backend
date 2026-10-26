namespace LoungeClubsApi.Features.Establishments.SuspendEstablishment;

public class SuspendEstablishmentCommandValidator : AbstractValidator<SuspendEstablishmentCommand>
{
    public SuspendEstablishmentCommandValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000)
            .WithMessage("A suspension reason is required.");
    }
}
