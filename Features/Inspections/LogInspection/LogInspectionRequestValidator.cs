namespace LoungeClubsApi.Features.Inspections.LogInspection;

public class LogInspectionRequestValidator : AbstractValidator<LogInspectionRequest>
{
    public LogInspectionRequestValidator()
    {
        RuleFor(x => x.Outcome).IsInEnum();
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleForEach(x => x.EvidenceUrls)
            .NotEmpty().MaximumLength(500)
            .When(x => x.EvidenceUrls != null);
    }
}
