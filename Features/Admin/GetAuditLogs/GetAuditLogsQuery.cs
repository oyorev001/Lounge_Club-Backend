using LoungeClubsApi.Models.Shared;

namespace LoungeClubsApi.Features.Admin.GetAuditLogs;

public record GetAuditLogsQuery(
    string? TableName,
    string? UserId,
    DateTimeOffset? From,
    DateTimeOffset? To,
        [property: FromQuery(Name = "type")] string? Type,
    [property: FromQuery(Name = "page")] int? Page,
    [property: FromQuery(Name = "per_page")] int? PageSize
) : IQuery<PaginatedResponse<AuditLogResponse>>;
