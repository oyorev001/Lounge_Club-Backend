namespace LoungeClubsApi.Features.Levies.GenerateAnnualLevies;

public record GenerateLeviesResponse(
    int Year,
    int Created,
    int Skipped);
