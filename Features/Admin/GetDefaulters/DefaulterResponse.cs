using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Admin.GetDefaulters;

public record DefaulterResponse(
    Guid Id,
    string RegistrationNumber,
    string Name,
    EstablishmentType Type,
    string Lga,
    string? TierCode,
    string OwnerName,
    string? OwnerPhone,
    string? OwnerEmail,
    int OverdueLeviesCount,
    int DaysSinceExpiryOrDue);
