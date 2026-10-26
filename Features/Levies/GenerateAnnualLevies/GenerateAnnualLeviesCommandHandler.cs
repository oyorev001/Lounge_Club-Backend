using LoungeClubsApi.Data;
using LoungeClubsApi.Entities;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Helpers;
using LoungeClubsApi.Interfaces;
using LoungeClubsApi.Services;

namespace LoungeClubsApi.Features.Levies.GenerateAnnualLevies;

public class GenerateAnnualLeviesCommandHandler(
    AppDbContext db,
    ICurrentUserService currentUser,
    NotificationService notificationService,
    Microsoft.Extensions.Options.IOptions<Configuration.AppSettings> appSettings)
    : ICommandHandler<GenerateAnnualLeviesCommand, GenerateLeviesResponse>
{
    public async Task<GenerateLeviesResponse> Handle(GenerateAnnualLeviesCommand request, CancellationToken cancellationToken)
    {
        var establishments = await db.Establishments
            .Include(e => e.Owner)
            .Include(e => e.Tier)
            // Suspended/NonCompliant venues still owe the statutory levy —
            // only deregistered/pending/rejected ones are exempt. Otherwise a
            // single critical inspection permanently silences the revenue.
            .Where(e => (e.Status == EstablishmentStatus.Approved
                || e.Status == EstablishmentStatus.Suspended
                || e.Status == EstablishmentStatus.NonCompliant)
                && e.TierId != null)
            .ToListAsync(cancellationToken);

        var alreadyAssessed = (await db.LevyAssessments
            .Where(l => l.Year == request.Year)
            .Select(l => l.EstablishmentId)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var sequence = await db.LevyAssessments.CountAsync(l => l.Year == request.Year, cancellationToken) + 1;

        // Statutory deadline April 30, but never issue an already-overdue levy —
        // levies generated late get a grace window instead.
        var dueDate = new DateOnly(request.Year, 4, 30);
        var graceDeadline = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(appSettings.Value.LevyDueDays));
        if (dueDate < graceDeadline && request.Year <= DateTime.UtcNow.Year)
            dueDate = graceDeadline;

        var created = new List<LevyAssessment>();

        foreach (var establishment in establishments)
        {
            if (alreadyAssessed.Contains(establishment.Id))
            {
                continue;
            }

            created.Add(new LevyAssessment
            {
                Reference = ReferenceGenerator.Yearly("LEV", request.Year, sequence++),
                EstablishmentId = establishment.Id,
                Establishment = establishment,
                TierId = establishment.TierId!.Value,
                Tier = establishment.Tier!,
                Year = request.Year,
                Amount = establishment.Tier!.AnnualFee,
                DueDate = dueDate,
                Status = LevyStatus.Pending
            });
        }

        db.LevyAssessments.AddRange(created);
        await db.SaveChangesWithUniqueRetryAsync(async () =>
        {
            // A concurrent run may have assessed some of the same establishments
            // (unique index on EstablishmentId+Year) — drop those entities so
            // the retry doesn't keep violating the constraint, then re-number.
            var nowAssessed = (await db.LevyAssessments
                .Where(l => l.Year == request.Year)
                .Select(l => l.EstablishmentId)
                .ToListAsync(cancellationToken))
                .ToHashSet();
            foreach (var colliding in created.Where(l => nowAssessed.Contains(l.EstablishmentId)).ToList())
            {
                db.Entry(colliding).State = EntityState.Detached;
                created.Remove(colliding);
            }

            var next = await db.LevyAssessments
                .CountAsync(l => l.Year == request.Year, cancellationToken) + 1;
            foreach (var levy in created)
                levy.Reference = ReferenceGenerator.Yearly("LEV", request.Year, next++);
        }, cancellationToken);

        // One SaveChanges for the whole batch — NotifyAsync saves per call.
        await notificationService.NotifyManyAsync(
            created.Select(a =>
                (a.Establishment.OwnerId,
                 $"Annual levy assessment for {request.Year}",
                 $"A levy of ₦{a.Amount:N2} ({a.Reference}) has been assessed for \"{a.Establishment.Name}\" for {request.Year}. Payment is due by {a.DueDate:MMMM d, yyyy}.",
                 (string?)$"/dashboard/levies?focus={a.Id}",
                 (string?)a.Establishment.Owner.Email)),
            NotificationType.LevyDue,
            cancellationToken);

        return new GenerateLeviesResponse(
            request.Year,
            created.Count,
            establishments.Count - created.Count);
    }
}
