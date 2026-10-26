using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Configuration;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Interfaces;
using Microsoft.Extensions.Options;
using LoungeClubsApi.Services;

namespace LoungeClubsApi.Features.Fines.WaiveFine;

public class WaiveFineCommandHandler(
    AppDbContext db,
    IHttpContextAccessor httpContextAccessor,
    IOptions<AppSettings> appSettings,
    ICurrentUserService currentUser,
    NotificationService notificationService)
    : ICommandHandler<WaiveFineCommand>
{
    public async Task Handle(WaiveFineCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("You must be logged in.");

        var fine = await db.Fines
            .Include(f => f.Establishment)
                .ThenInclude(e => e.Owner)
            .FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Fine not found.");

        // Waivers above the configured threshold need Admin/DG sign-off.
        var threshold = appSettings.Value.WaiverApprovalThreshold;
        if (fine.Amount > threshold)
        {
            var principal = httpContextAccessor.HttpContext?.User;
            var allowed = principal?.IsInRole(nameof(AppRole.Admin)) == true
                ;
            if (!allowed)
                throw new UnauthorizedAccessException(
                    $"Waiving ₦{fine.Amount:N0} requires Admin or Director General approval (threshold: ₦{threshold:N0}).");
        }

        if (fine.Status != FineStatus.Unpaid)
            throw new InvalidOperationException("Only unpaid fines can be waived.");

        fine.Status = FineStatus.Waived;
        fine.WaivedById = userId;
        fine.WaiverReason = request.Reason;

        // Cancel open checkouts so an in-flight payment can't settle a waived fine.
        var openCheckouts = await db.Transactions
            .Where(t => t.Context == TransactionContext.Fine
                && t.ContextId == fine.Id
                && t.Status == PaymentStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var open in openCheckouts)
        {
            open.Status = PaymentStatus.Cancelled;
            open.FailureReason = "Fine waived.";
        }

        await db.SaveChangesAsync(cancellationToken);

        await notificationService.NotifyAsync(
            fine.Establishment.OwnerId,
            NotificationType.General,
            $"Fine waived: {fine.Reference}",
            $"The fine {fine.Reference} of ₦{fine.Amount:N0} issued to \"{fine.Establishment.Name}\" has been waived. Reason: {fine.WaiverReason}",
            $"/dashboard/fines?focus={fine.Id}",
            fine.Establishment.Owner.Email,
            cancellationToken);
    }
}
