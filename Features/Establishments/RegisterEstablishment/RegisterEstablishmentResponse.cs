using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Establishments.RegisterEstablishment;

public record RegisterEstablishmentResponse(
    Guid Id,
    string RegistrationNumber,
    string Name,
    EstablishmentType Type,
    List<EstablishmentType> AdditionalTypes,
    EstablishmentStatus Status,
    int SuggestedTierScore,
    string? SuggestedTierName);
