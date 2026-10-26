using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Configuration;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Interfaces;
using LoungeClubsApi.Services;
using Microsoft.Extensions.Options;

namespace LoungeClubsApi.Features.Levies.WaiveLevy;

public class WaiveLevyCommandHandler(
    AppDbContext db,
    JurisdictionService jurisdiction,
    ICurrentUserService currentUser,
    NotificationService notificationService,
    IHttpContextAccessor httpContextAccessor,
    IOptions<AppSettings> appSettings)
    : ICommandHandler<WaiveLevyCommand, WaiveLevyResponse>
{
    public async Task<WaiveLevyResponse> Handle(WaiveLevyCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("You must be logged in.");

        var levy = await db.LevyAssessments
            .Include(l => l.Establishment)
                .ThenInclude(e => e.Owner)
            .FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Levy assessment not found.");
        await jurisdiction.EnsureCanActOnAsync(levy.Establishment.Lga, cancellationToken);

        // Waivers above the configured threshold need Admin/DG sign-off.
        var threshold = appSettings.Value.WaiverApprovalThreshold;
        if (levy.Amount > threshold)
        {
            var principal = httpContextAccessor.HttpContext?.User;
            var allowed = principal?.IsInRole(nameof(AppRole.Admin)) == true
                ;
            if (!allowed)
                throw new UnauthorizedAccessException(
                    $"Waiving ₦{levy.Amount:N0} requires Admin or Director General approval (threshold: ₦{threshold:N0}).");
        }

        if (levy.Status is not (LevyStatus.Pending or LevyStatus.Overdue))
        {
            throw new InvalidOperationException($"Only pending or overdue levies can be waived. Current status: {levy.Status}.");
        }

        levy.Status = LevyStatus.Waived;
        levy.WaivedById = userId;
        levy.WaiverReason = request.Reason;

        // Cancel open checkouts so an in-flight payment can't settle a waived levy.
        var openCheckouts = await db.Transactions
            .Where(t => t.Context == TransactionContext.Levy
                && t.ContextId == levy.Id
                && t.Status == PaymentStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var open in openCheckouts)
        {
            open.Status = PaymentStatus.Cancelled;
            open.FailureReason = "Levy waived.";
        }

        await db.SaveChangesAsync(cancellationToken);

        await notificationService.NotifyAsync(
            levy.Establishment.OwnerId,
            NotificationType.LevyDue,
            $"Levy {levy.Reference} waived",
            $"Your levy assessment {levy.Reference} for \"{levy.Establishment.Name}\" ({levy.Year}) has been waived. Reason: {request.Reason}",
            $"/dashboard/levies?focus={levy.Id}",
            emailTo: levy.Establishment.Owner.Email,
            cancellationToken: cancellationToken);

        return new WaiveLevyResponse(levy.Id, levy.Status);
    }
}
