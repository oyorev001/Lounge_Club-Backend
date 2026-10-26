namespace LoungeClubsApi.Features.Establishments.ApproveEstablishment;

public record ApproveEstablishmentCommand(
    Guid EstablishmentId,
    Guid TierId,
    string? Notes
) : ICommand<ApproveEstablishmentResponse>;
