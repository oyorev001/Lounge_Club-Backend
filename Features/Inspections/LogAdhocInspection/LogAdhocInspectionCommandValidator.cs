namespace LoungeClubsApi.Features.Inspections.LogAdhocInspection;

public class LogAdhocInspectionCommandValidator : AbstractValidator<LogAdhocInspectionCommand>
{
    public LogAdhocInspectionCommandValidator()
    {
        RuleFor(x => x.EstablishmentId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Outcome).IsInEnum();
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleForEach(x => x.EvidenceUrls)
            .NotEmpty().MaximumLength(500)
            .When(x => x.EvidenceUrls != null);
    }
}
