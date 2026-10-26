namespace LoungeClubsApi.Features.Inspections.ScheduleInspection;

public class ScheduleInspectionCommandValidator : AbstractValidator<ScheduleInspectionCommand>
{
    public ScheduleInspectionCommandValidator()
    {
        RuleFor(x => x.EstablishmentId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.ScheduledFor).GreaterThan(DateTimeOffset.UtcNow)
            .WithMessage("Inspection must be scheduled for a future date/time.");
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
