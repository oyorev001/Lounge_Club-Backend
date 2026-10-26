using LoungeClubsApi.Data;
using LoungeClubsApi.Entities;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Helpers;
using LoungeClubsApi.Interfaces;
using LoungeClubsApi.Services;

namespace LoungeClubsApi.Features.Establishments.RegisterEstablishment;

public class RegisterEstablishmentCommandHandler(
    AppDbContext db,
    ICurrentUserService currentUser,
    TierSuggestionService tierSuggestion,
    NotificationService notificationService)
    : ICommandHandler<RegisterEstablishmentCommand, RegisterEstablishmentResponse>
{
    public async Task<RegisterEstablishmentResponse> Handle(RegisterEstablishmentCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("You must be logged in.");

        // Staff-initiated registration on behalf of a business owner (walk-ins
        // at the ministry desk). Only Admin can act on behalf of another user.
        var effectiveOwnerId = userId;
        if (!string.IsNullOrWhiteSpace(request.OnBehalfOfUserId) && request.OnBehalfOfUserId != userId)
        {
            if (!currentUser.IsInRole(nameof(Authorization.Enums.AppRole.Admin)))
                throw new UnauthorizedAccessException("Only administrators can register on behalf of another user.");
            var target = await db.Users.FindAsync(request.OnBehalfOfUserId)
                ?? throw new KeyNotFoundException("The specified owner account was not found.");
            effectiveOwnerId = target.Id;
        }

        // Dedupe: one establishment per CAC registration number. NIN reuse is
        // allowed (an owner may run several venues) but flagged for officers.
        var normalizedCac = request.CacNumber.Trim().ToUpperInvariant();
        var cacTaken = await db.Establishments
            .AnyAsync(e => e.CacNumber.ToUpper() == normalizedCac, cancellationToken);
        if (cacTaken)
            throw new InvalidOperationException(
                "An establishment with this CAC number is already registered. Contact the ministry if this looks wrong.");

        // Sequential number from MAX(existing) — NOT Count(), which collides
        // when an establishment is withdrawn/deleted.
        async Task<int> NextSequenceAsync() =>
            (await db.Establishments
                .Select(e => e.RegistrationNumber)
                .ToListAsync(cancellationToken))
            .Select(n => int.TryParse(n.Split('-').LastOrDefault(), out var v) ? v : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;

        var primaryType = request.Types.PrimaryOf();

        var establishment = new Establishment
        {
            RegistrationNumber = ReferenceGenerator.Sequential("EST", await NextSequenceAsync()),
            Name = request.Name,
            Type = primaryType,
            AdditionalTypes = request.Types.Where(t => t != primaryType).ToList(),
            Description = request.Description,
            Address = request.Address,
            Lga = request.Lga,
            Landmark = request.Landmark,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            ContactPhone = request.ContactPhone,
            ContactEmail = request.ContactEmail,
            CacNumber = request.CacNumber,
            OwnerNin = request.Nin,
            Capacity = request.Capacity,
            StaffCount = request.StaffCount,
            FloorAreaSqm = request.FloorAreaSqm,
            HasLiveBand = request.HasLiveBand,
            HasPoolTable = request.HasPoolTable,
            HasRestaurant = request.HasRestaurant,
            HasOutdoorSpace = request.HasOutdoorSpace,
            HasDj = request.HasDj,
            HasVipArea = request.HasVipArea,
            HasShisha = request.HasShisha,
            HasRooftop = request.HasRooftop,
            HasLodging = request.HasLodging,
            HasParking = request.HasParking,
            ParkingSpaces = request.ParkingSpaces,
            OperatingHours = request.OperatingHours,
            OwnerId = effectiveOwnerId,
            Status = EstablishmentStatus.PendingReview
        };

        establishment.SuggestedTierScore = tierSuggestion.ComputeScore(establishment);

        db.Establishments.Add(establishment);
        await db.SaveChangesWithUniqueRetryAsync(async () =>
        {
            establishment.RegistrationNumber = ReferenceGenerator.Sequential("EST", await NextSequenceAsync());
        }, cancellationToken);

        var suggestedTier = await tierSuggestion.SuggestTierAsync(establishment.SuggestedTierScore.Value, cancellationToken);

        await notificationService.NotifyAsync(
            effectiveOwnerId,
            NotificationType.ApplicationStatus,
            "Establishment registration received",
            $"Your registration of \"{establishment.Name}\" ({establishment.RegistrationNumber}) has been received and is pending officer review.",
            $"/dashboard/establishments/{establishment.Id}",
            cancellationToken: cancellationToken);

        return new RegisterEstablishmentResponse(
            establishment.Id,
            establishment.RegistrationNumber,
            establishment.Name,
            establishment.Type,
            establishment.AdditionalTypes,
            establishment.Status,
            establishment.SuggestedTierScore.Value,
            suggestedTier?.Name);
    }
}
