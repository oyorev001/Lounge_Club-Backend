namespace LoungeClubsApi.Features.Admin.GetAuditLogs;

public record AuditLogResponse(
    Guid Id,
    string? UserId,
    string Type,
    string TableName,
    string PrimaryKey,
    string? OldValues,
    string? NewValues,
    string? IpAddress,
    string? UserAgent,
    DateTimeOffset Timestamp);
