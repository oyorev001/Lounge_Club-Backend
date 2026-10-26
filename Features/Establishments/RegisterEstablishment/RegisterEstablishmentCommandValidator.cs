using LoungeClubsApi.Helpers;

namespace LoungeClubsApi.Features.Establishments.RegisterEstablishment;

public class RegisterEstablishmentCommandValidator : AbstractValidator<RegisterEstablishmentCommand>
{
    public RegisterEstablishmentCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Types).NotEmpty().WithMessage("Select at least one establishment type.");
        RuleForEach(x => x.Types).IsInEnum();
        RuleFor(x => x.Types)
            .Must(t => t.Distinct().Count() == t.Count)
            .WithMessage("Duplicate establishment types are not allowed.")
            .When(x => x.Types is { Count: > 0 });
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Lga).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Latitude).NotNull().WithMessage("Venue latitude is required — pin it on the map.");
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).NotNull().WithMessage("Venue longitude is required — pin it on the map.");
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue);
        RuleFor(x => x.ContactPhone).NotEmpty().MaximumLength(20);
        RuleFor(x => x.ContactEmail).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));
        RuleFor(x => x.CacNumber).NotEmpty().MaximumLength(50)
            .WithMessage("A CAC registration number is required.");
        RuleFor(x => x.Nin).NotEmpty().Matches(@"^\d{11}$")
            .WithMessage("NIN must be exactly 11 digits.")
            .Must(Helpers.NinRules.IsPlausible)
            .WithMessage("Enter a valid NIN — placeholder or repeated digits are not accepted.");
        RuleFor(x => x.Capacity).GreaterThan(0);
        RuleFor(x => x.StaffCount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.FloorAreaSqm).GreaterThan(0).When(x => x.FloorAreaSqm.HasValue);
        RuleFor(x => x.ParkingSpaces).GreaterThanOrEqualTo(0).When(x => x.ParkingSpaces.HasValue);
        RuleFor(x => x.ParkingSpaces).NotNull().When(x => x.HasParking)
            .WithMessage("Parking spaces count is required when the venue has parking.");
    }
}
