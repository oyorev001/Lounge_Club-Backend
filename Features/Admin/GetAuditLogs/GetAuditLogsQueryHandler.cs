using LoungeClubsApi.Data;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Models.Shared;

namespace LoungeClubsApi.Features.Admin.GetAuditLogs;

public class GetAuditLogsQueryHandler(AppDbContext db)
    : IQueryHandler<GetAuditLogsQuery, PaginatedResponse<AuditLogResponse>>
{
    public async Task<PaginatedResponse<AuditLogResponse>> Handle(GetAuditLogsQuery query, CancellationToken cancellationToken)
    {
        var queryable = db.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Type))
            queryable = queryable.Where(a => a.Type == query.Type);
        if (!string.IsNullOrWhiteSpace(query.TableName))
            queryable = queryable.Where(a => a.TableName == query.TableName);

        if (!string.IsNullOrWhiteSpace(query.UserId))
            queryable = queryable.Where(a => a.UserId == query.UserId);

        if (query.From.HasValue)
            queryable = queryable.Where(a => a.Timestamp >= query.From.Value);

        if (query.To.HasValue)
        {
            // A date-only `to` means "through that day" — include it fully.
            var to = query.To.Value.TimeOfDay == TimeSpan.Zero
                ? query.To.Value.AddDays(1).AddTicks(-1)
                : query.To.Value;
            queryable = queryable.Where(a => a.Timestamp <= to);
        }

        var projected = queryable
            .OrderByDescending(a => a.Timestamp)
            .Select(a => new AuditLogResponse(
                a.Id,
                a.UserId,
                a.Type,
                a.TableName,
                a.PrimaryKey,
                a.OldValues,
                a.NewValues,
                a.IpAddress,
                a.UserAgent,
                a.Timestamp));

        var pagedList = await PagedList<AuditLogResponse>.CreateAsync(projected, query.Page ?? 1, query.PageSize ?? 15);

        return pagedList.ToPaginatedResponse(items => items, "admin/audit-logs");
    }
}
