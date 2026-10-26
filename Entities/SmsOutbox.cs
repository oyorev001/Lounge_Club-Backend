using LoungeClubsApi.Entities.Base;

namespace LoungeClubsApi.Entities;

/// <summary>Queued SMS — drained by SmsOutboxJob with retry/backoff.</summary>
public class SmsOutbox : BaseEntity
{
    public string To { get; set; } = string.Empty;      // MSISDN e.g. 2348012345678
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";     // Pending | Sent | Failed
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset NextAttemptAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SentAt { get; set; }
}
