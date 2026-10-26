using FluentValidation;

namespace LoungeClubsApi.Configuration.Validators;

public sealed class DatabaseSettingsValidator : AbstractValidator<DatabaseSettings>
{
    private static readonly string[] AllowedProviders = ["mssql", "mysql", "postgresql", "postgres", "npgsql"];

    public DatabaseSettingsValidator()
    {
        RuleFor(x => x.DBProvider)
            .NotEmpty()
            .WithMessage($"{nameof(DatabaseSettings.DBProvider)} is required")
            .Must(provider => AllowedProviders.Contains(provider, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"{nameof(DatabaseSettings.DBProvider)} must be one of: {string.Join(", ", AllowedProviders)}");
    }
}
