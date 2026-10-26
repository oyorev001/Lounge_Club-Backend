namespace LoungeClubsApi.Features.Establishments.WithdrawEstablishment;

public record WithdrawEstablishmentCommand(
    Guid EstablishmentId
) : ICommand<WithdrawEstablishmentResponse>;

public record WithdrawEstablishmentResponse(Guid Id);
