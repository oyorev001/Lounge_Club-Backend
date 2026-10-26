using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Establishments.GetAllEstablishments;

public record EstablishmentListItem(
    Guid Id,
    string RegistrationNumber,
    string Name,
    EstablishmentType Type,
    List<EstablishmentType> AdditionalTypes,
    EstablishmentStatus Status,
    string Lga,
    string Address,
    string? TierCode,
    string? TierName,
    int? SuggestedTierScore,
    DateTimeOffset CreatedAt);
