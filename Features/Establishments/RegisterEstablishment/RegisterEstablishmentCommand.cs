using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Establishments.RegisterEstablishment;

public record RegisterEstablishmentCommand(
    string Name,
    List<EstablishmentType> Types,
    string? Description,
    string Address,
    string Lga,
    string? Landmark,
    double? Latitude,
    double? Longitude,
    string ContactPhone,
    string? ContactEmail,
    string CacNumber,
    string Nin,
    int Capacity,
    int StaffCount,
    int? FloorAreaSqm,
    bool HasLiveBand,
    bool HasPoolTable,
    bool HasRestaurant,
    bool HasOutdoorSpace,
    bool HasDj,
    bool HasVipArea,
    bool HasShisha,
    bool HasRooftop,
    bool HasLodging,
    bool HasParking,
    int? ParkingSpaces,
    string? OperatingHours,
    /// <summary>Staff-initiated registration on behalf of an owner (walk-ins).</summary>
    string? OnBehalfOfUserId
) : ICommand<RegisterEstablishmentResponse>;
