using LoungeClubsApi.Data;
using LoungeClubsApi.Entities;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Models.Shared;

namespace LoungeClubsApi.Features.Admin.GetDefaulters;

public class GetDefaultersQueryHandler(AppDbContext db)
    : IQueryHandler<GetDefaultersQuery, PaginatedResponse<DefaulterResponse>>
{
    public async Task<PaginatedResponse<DefaulterResponse>> Handle(GetDefaultersQuery query, CancellationToken cancellationToken)
    {
        // Approved establishments with no active certificate, or with overdue levies
        var queryable = db.Establishments
            .AsNoTracking()
            .Include(e => e.Owner)
            .Include(e => e.Tier)
            .Include(e => e.Certificates)
            .Include(e => e.LevyAssessments.Where(l => l.Status == LevyStatus.Overdue))
            .Where(e => e.Status == EstablishmentStatus.Approved
                && (!e.Certificates.Any(c => c.Status == CertificateStatus.Active)
                    || e.LevyAssessments.Any(l => l.Status == LevyStatus.Overdue)))
            .OrderBy(e => e.Name);

        var pagedList = await PagedList<Establishment>.CreateAsync(queryable, query.Page ?? 1, query.PageSize ?? 15);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return pagedList.ToPaginatedResponse(items => items.Select(e =>
        {
            var overdueLevies = e.LevyAssessments.ToList();

            var ownerName = $"{e.Owner.FirstName} {e.Owner.LastName}".Trim();
            if (ownerName.Length == 0)
                ownerName = e.Owner.UserName ?? e.Owner.Email ?? string.Empty;

            var oldestDue = overdueLevies
                .OrderBy(l => l.DueDate)
                .Select(l => (DateOnly?)l.DueDate)
                .FirstOrDefault();
            var lastExpiry = e.Certificates
                .OrderByDescending(c => c.ExpiryDate)
                .Select(c => (DateOnly?)c.ExpiryDate)
                .FirstOrDefault();

            var daysSince = oldestDue.HasValue
                ? today.DayNumber - oldestDue.Value.DayNumber
                : lastExpiry.HasValue
                    ? today.DayNumber - lastExpiry.Value.DayNumber
                    : e.ApprovedAt.HasValue
                        ? (int)(DateTimeOffset.UtcNow - e.ApprovedAt.Value).TotalDays
                        : 0;

            return new DefaulterResponse(
                e.Id,
                e.RegistrationNumber,
                e.Name,
                e.Type,
                e.Lga,
                e.Tier?.Code,
                ownerName,
                e.Owner.PhoneNumber ?? e.ContactPhone,
                e.Owner.Email ?? e.ContactEmail,
                overdueLevies.Count,
                Math.Max(daysSince, 0));
        }).ToList(), "admin/defaulters");
    }
}
