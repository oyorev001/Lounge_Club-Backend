namespace LoungeClubsApi.Features.Establishments.SuspendEstablishment;

public record SuspendEstablishmentCommand(
    Guid EstablishmentId,
    string Reason
) : ICommand<SuspendEstablishmentResponse>;
