namespace LoungeClubsApi.Features.Levies.WaiveLevy;

public record WaiveLevyCommand(
    Guid Id,
    string Reason
) : ICommand<WaiveLevyResponse>;
