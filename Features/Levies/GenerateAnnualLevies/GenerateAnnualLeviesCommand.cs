namespace LoungeClubsApi.Features.Levies.GenerateAnnualLevies;

public record GenerateAnnualLeviesCommand(
    int Year
) : ICommand<GenerateLeviesResponse>;
