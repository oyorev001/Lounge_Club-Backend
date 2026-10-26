using LoungeClubsApi.Entities.Base;

namespace LoungeClubsApi.Entities;

public class WebhookLog : BaseEntity
{
    public string Provider { get; set; } = string.Empty; // Paystack
    /// <summary>Provider-scoped event id ("{eventType}:{data.id}") — unique index dedupes retries.</summary>
    public string? EventId { get; set; }
    public string? EventType { get; set; }
    public string Payload { get; set; } = string.Empty;
    public string? Signature { get; set; }
    public bool SignatureValid { get; set; }
    public bool Processed { get; set; }
    public string? ProcessingResult { get; set; }
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
}
