namespace LoungeClubsApi.Features.Establishments.RejectEstablishment;

public record RejectEstablishmentCommand(
    Guid EstablishmentId,
    string Reason
) : ICommand<RejectEstablishmentResponse>;
