using LoungeClubsApi.Data;
using LoungeClubsApi.Entities;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Helpers;
using LoungeClubsApi.Interfaces;
using LoungeClubsApi.Services;

namespace LoungeClubsApi.Features.Fines.IssueFine;

public class IssueFineCommandHandler(
    AppDbContext db,
    ICurrentUserService currentUser,
    NotificationService notificationService)
    : ICommandHandler<IssueFineCommand, IssueFineResponse>
{
    public async Task<IssueFineResponse> Handle(IssueFineCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("You must be logged in.");

        var establishment = await db.Establishments
            .Include(e => e.Owner)
            .FirstOrDefaultAsync(e => e.Id == request.EstablishmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Establishment not found.");

        // Fines only apply to operating venues — pending/rejected/deregistered
        // establishments shouldn't receive enforcement penalties.
        if (establishment.Status is not (EstablishmentStatus.Approved
            or EstablishmentStatus.Suspended or EstablishmentStatus.NonCompliant))
            throw new InvalidOperationException(
                $"Cannot fine an establishment in {establishment.Status} status.");

        if (request.InspectionId.HasValue
            && !await db.Inspections.AnyAsync(
                i => i.Id == request.InspectionId.Value && i.EstablishmentId == establishment.Id,
                cancellationToken))
        {
            throw new InvalidOperationException("Linked inspection not found for this establishment.");
        }

        var count = await db.Fines.CountAsync(cancellationToken) + 1;

        var fine = new Fine
        {
            Reference = ReferenceGenerator.Sequential("FIN", count),
            EstablishmentId = establishment.Id,
            IssuedById = userId,
            InspectionId = request.InspectionId,
            FineType = request.FineType,
            Amount = request.Amount,
            Reason = request.Reason,
            Status = FineStatus.Unpaid
        };

        db.Fines.Add(fine);
        await db.SaveChangesWithUniqueRetryAsync(async () =>
        {
            var next = await db.Fines.CountAsync(cancellationToken) + 1;
            fine.Reference = ReferenceGenerator.Sequential("FIN", next);
        }, cancellationToken);

        await notificationService.NotifyAsync(
            establishment.OwnerId,
            NotificationType.FineIssued,
            $"Fine issued: {fine.Reference}",
            $"A fine of ₦{fine.Amount:N0} ({fine.FineType}) has been issued to \"{establishment.Name}\". Reason: {fine.Reason}",
            $"/dashboard/fines?focus={fine.Id}",
            establishment.Owner.Email,
            cancellationToken);

        return new IssueFineResponse(
            fine.Id,
            fine.Reference,
            fine.Amount,
            fine.Status);
    }
}
