namespace LoungeClubsApi.Features.Establishments.ReinstateEstablishment;

public record ReinstateEstablishmentCommand(
    Guid EstablishmentId,
    string? Notes
) : ICommand<ReinstateEstablishmentResponse>;

public record ReinstateEstablishmentResponse(Guid EstablishmentId, string Status);
