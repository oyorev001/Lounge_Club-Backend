using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Establishments.RejectEstablishment;

public record RejectEstablishmentResponse(
    Guid Id,
    EstablishmentStatus Status);
