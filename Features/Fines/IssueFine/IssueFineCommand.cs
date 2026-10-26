namespace LoungeClubsApi.Features.Fines.IssueFine;

public record IssueFineCommand(
    Guid EstablishmentId,
    string FineType,
    decimal Amount,
    string Reason,
    Guid? InspectionId
) : ICommand<IssueFineResponse>;
