using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Establishments.CloseEstablishment;

public record CloseEstablishmentCommand(
    Guid EstablishmentId,
    string? Reason
) : ICommand<CloseEstablishmentResponse>;

public record CloseEstablishmentResponse(Guid Id, EstablishmentStatus Status);
