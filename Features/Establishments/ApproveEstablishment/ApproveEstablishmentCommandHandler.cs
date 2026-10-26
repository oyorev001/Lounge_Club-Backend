using LoungeClubsApi.Configuration;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Helpers;
using LoungeClubsApi.Interfaces;
using LoungeClubsApi.Services;
using Microsoft.Extensions.Options;

namespace LoungeClubsApi.Features.Establishments.ApproveEstablishment;

public class ApproveEstablishmentCommandHandler(
    AppDbContext db,
    JurisdictionService jurisdiction,
    ICurrentUserService currentUser,
    NotificationService notificationService,
    IOptions<AppSettings> appSettings)
    : ICommandHandler<ApproveEstablishmentCommand, ApproveEstablishmentResponse>
{
    public async Task<ApproveEstablishmentResponse> Handle(ApproveEstablishmentCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("You must be logged in.");

        var establishment = await db.Establishments
            .Include(e => e.Owner)
            .Include(e => e.Documents)
            .FirstOrDefaultAsync(e => e.Id == request.EstablishmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Establishment not found.");
        await jurisdiction.EnsureCanActOnAsync(establishment.Lga, cancellationToken);

        if (establishment.Status is not (EstablishmentStatus.PendingReview or EstablishmentStatus.Rejected))
            throw new InvalidOperationException($"Only establishments pending review or previously rejected can be approved. Current status: {establishment.Status}.");

        if (string.IsNullOrWhiteSpace(establishment.CacNumber))
            throw new InvalidOperationException("A CAC registration number is required before approval.");

        if (string.IsNullOrWhiteSpace(establishment.OwnerNin))
            throw new InvalidOperationException("The owner's NIN is required before approval.");

        if (!establishment.Documents.Any(d => d.DocumentType == DocumentType.CacCertificate))
            throw new InvalidOperationException("A CAC certificate document must be uploaded before approval.");

        var tier = await db.LicenseTiers
            .FirstOrDefaultAsync(t => t.Id == request.TierId, cancellationToken)
            ?? throw new KeyNotFoundException("License tier not found.");

        if (!tier.IsActive)
            throw new InvalidOperationException("The selected license tier is inactive.");

        var now = DateTime.UtcNow;

        establishment.Status = EstablishmentStatus.Approved;
        establishment.ApprovedAt = now;
        establishment.ApprovedById = userId;
        establishment.TierId = tier.Id;
        establishment.RejectionReason = null;

        // Auto-generate the levy assessment for the current year (once per establishment per year)
        var year = now.Year;
        var levy = await db.LevyAssessments
            .FirstOrDefaultAsync(l => l.EstablishmentId == establishment.Id && l.Year == year, cancellationToken);

        if (levy is null)
        {
            var countForYear = await db.LevyAssessments.CountAsync(l => l.Year == year, cancellationToken);

            levy = new LevyAssessment
            {
                Reference = ReferenceGenerator.Yearly("LEV", year, countForYear + 1),
                EstablishmentId = establishment.Id,
                TierId = tier.Id,
                Year = year,
                Amount = tier.AnnualFee,
                DueDate = DateOnly.FromDateTime(now.AddDays(appSettings.Value.LevyDueDays)),
                Status = LevyStatus.Pending
            };

            db.LevyAssessments.Add(levy);
        }

        var levyIsNew = db.Entry(levy).State == EntityState.Added;

        await db.SaveChangesWithUniqueRetryAsync(async () =>
        {
            // Only re-number a levy created this call — renaming a pre-existing
            // persisted reference on collision would corrupt its public ID.
            if (levyIsNew)
            {
                var next = await db.LevyAssessments.CountAsync(l => l.Year == year, cancellationToken) + 1;
                levy.Reference = ReferenceGenerator.Yearly("LEV", year, next);
            }
        }, cancellationToken);

        var notes = string.IsNullOrWhiteSpace(request.Notes) ? "" : $" Officer notes: {request.Notes}";
        var levyMsg = levyIsNew
            ? $" A {year} levy assessment of ₦{levy.Amount:N2} has been issued, due {levy.DueDate:dd MMM yyyy}."
            : levy.Status is LevyStatus.Pending or LevyStatus.Overdue
                ? $" Your existing {year} levy of ₦{levy.Amount:N2} is due {levy.DueDate:dd MMM yyyy}."
                : "";

        await notificationService.NotifyAsync(
            establishment.OwnerId,
            NotificationType.ApplicationStatus,
            "Establishment approved",
            $"Your establishment \"{establishment.Name}\" ({establishment.RegistrationNumber}) has been approved under the {tier.Name} tier.{levyMsg}{notes}",
            $"/dashboard/establishments/{establishment.Id}",
            establishment.Owner.Email,
            cancellationToken);

        return new ApproveEstablishmentResponse(
            establishment.Id,
            establishment.Status,
            tier.Name,
            levy.Id,
            levy.Amount);
    }
}
