using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Establishments.SuspendEstablishment;

public record SuspendEstablishmentResponse(
    Guid Id,
    EstablishmentStatus Status);
