using System.Security.Claims;
using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Interfaces;
using LoungeClubsApi.Services;

namespace LoungeClubsApi.Features.Establishments.WithdrawEstablishment;

public class WithdrawEstablishmentCommandHandler(
    AppDbContext db,
    ICurrentUserService currentUser,
    IHttpContextAccessor httpContextAccessor,
    IFileStorage fileStorage,
    NotificationService notificationService,
    ILogger<WithdrawEstablishmentCommandHandler> logger)
    : ICommandHandler<WithdrawEstablishmentCommand, WithdrawEstablishmentResponse>
{
    public async Task<WithdrawEstablishmentResponse> Handle(WithdrawEstablishmentCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("You must be logged in.");

        var establishment = await db.Establishments
            .Include(e => e.Owner)
            .Include(e => e.Documents)
            .FirstOrDefaultAsync(e => e.Id == request.EstablishmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Establishment not found.");

        if (establishment.OwnerId != userId && !IsStaff(httpContextAccessor.HttpContext?.User))
            throw new UnauthorizedAccessException("You do not have access to this establishment.");

        // Only applications that never went live can be removed entirely.
        if (establishment.Status is not (EstablishmentStatus.PendingReview or EstablishmentStatus.Rejected))
        {
            throw new InvalidOperationException(
                $"A {establishment.Status} establishment cannot be withdrawn — close it instead to preserve the audit trail.");
        }

        // Defense in depth: never delete anything with financial/operational history.
        var hasHistory = await db.LevyAssessments.AnyAsync(l => l.EstablishmentId == establishment.Id, cancellationToken)
            || await db.Certificates.AnyAsync(c => c.EstablishmentId == establishment.Id, cancellationToken)
            || await db.Fines.AnyAsync(f => f.EstablishmentId == establishment.Id, cancellationToken)
            || await db.Inspections.AnyAsync(i => i.EstablishmentId == establishment.Id, cancellationToken);
        if (hasHistory)
        {
            throw new InvalidOperationException(
                "This application has compliance or financial records attached and cannot be deleted.");
        }

        var documentKeys = establishment.Documents
            .Select(d => d.FileUrl)
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .ToList();

        db.Establishments.Remove(establishment);
        await db.SaveChangesAsync(cancellationToken);

        // Best-effort cleanup of stored files after the rows are gone.
        foreach (var key in documentKeys)
        {
            try
            {
                var storageKey = key.StartsWith("/files/") ? key["/files/".Length..] : key;
                await fileStorage.DeleteAsync(storageKey, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not delete stored document {Key}", key);
            }
        }

        if (establishment.OwnerId != userId)
        {
            await notificationService.NotifyAsync(
                establishment.OwnerId,
                NotificationType.ApplicationStatus,
                "Application withdrawn",
                $"Your registration application for \"{establishment.Name}\" ({establishment.RegistrationNumber}) was withdrawn by the ministry.",
                "/dashboard/establishments",
                establishment.Owner?.Email,
                cancellationToken);
        }

        return new WithdrawEstablishmentResponse(establishment.Id);
    }

    private static bool IsStaff(ClaimsPrincipal? user) => user is not null && (
        user.IsInRole(nameof(AppRole.Officer)) ||
        user.IsInRole(nameof(AppRole.Admin))   );
}
