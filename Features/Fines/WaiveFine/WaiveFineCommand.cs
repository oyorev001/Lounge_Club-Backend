namespace LoungeClubsApi.Features.Fines.WaiveFine;

public record WaiveFineCommand(
    Guid Id,
    string Reason
) : ICommand;
