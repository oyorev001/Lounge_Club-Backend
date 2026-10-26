using LoungeClubsApi.Data;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Models.Shared;

namespace LoungeClubsApi.Features.Certificates.GetAllCertificates;

public class GetAllCertificatesQueryHandler(AppDbContext db)
    : IQueryHandler<GetAllCertificatesQuery, PaginatedResponse<CertificateListItem>>
{
    public async Task<PaginatedResponse<CertificateListItem>> Handle(GetAllCertificatesQuery query, CancellationToken ct)
    {
        var r = query.Request;
        var queryable = db.Certificates
            .Include(c => c.Establishment)
            .Include(c => c.LevyAssessment).ThenInclude(a => a.Tier)
            .AsQueryable();

        if (r.Status.HasValue)
            queryable = queryable.Where(c => c.Status == r.Status.Value);
        if (r.EstablishmentId.HasValue)
            queryable = queryable.Where(c => c.EstablishmentId == r.EstablishmentId.Value);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (!string.IsNullOrWhiteSpace(r.Tier))
        {
            var t = r.Tier.Trim();
            queryable = queryable.Where(c =>
                c.LevyAssessment.Tier != null &&
                (c.LevyAssessment.Tier.Code == t || c.LevyAssessment.Tier.Name == t));
        }
        if (r.ExpiringInDays.HasValue)
            queryable = queryable.Where(c =>
                c.Status == CertificateStatus.Active &&
                c.ExpiryDate >= today &&
                c.ExpiryDate <= today.AddDays(r.ExpiringInDays.Value));
        if (r.IssuedWithinDays.HasValue)
            queryable = queryable.Where(c =>
                c.IssueDate >= today.AddDays(-r.IssuedWithinDays.Value));
        if (r.Year.HasValue)
            queryable = queryable.Where(c => c.LevyYear == r.Year.Value);
        if (!string.IsNullOrWhiteSpace(r.Search))
        {
            var s = r.Search.Trim();
            queryable = queryable.Where(c =>
                c.CertificateNumber.Contains(s) || c.Establishment.Name.Contains(s));
        }

        var paged = await PagedList<Entities.Certificate>.CreateAsync(
            queryable.OrderByDescending(c => c.IssueDate), r.Page ?? 1, r.PageSize ?? 15);

        return paged.ToPaginatedResponse(items => items.Select(c => new CertificateListItem(
            c.Id,
            c.CertificateNumber,
            c.Establishment.Name,
            c.Establishment.Type.ToString(),
            c.Establishment.Lga,
            c.LevyAssessment.Tier?.Name,
            c.LevyYear,
            c.AmountPaid,
            c.IssueDate,
            c.ExpiryDate,
            c.Status,
            c.Status == CertificateStatus.Active && c.ExpiryDate >= today)).ToList(), "certificates");
    }
}
