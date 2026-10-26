using LoungeClubsApi.Entities.Base;
using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Entities;

/// <summary>
/// Durable email queue — NotificationService writes rows here in the same unit
/// of work as the business change, and EmailOutboxJob drains them with retries.
/// SMTP slowness or outages can never block or silently drop user mail.
/// </summary>
public class EmailOutbox : BaseEntity
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;

    /// <summary>JSON-serialized EmailAttachment[] — receipt/cert PDFs, inline QR images.</summary>
    public string? Attachments { get; set; }
    public EmailOutboxStatus Status { get; set; } = EmailOutboxStatus.Pending;
    public int Attempts { get; set; }
    public DateTimeOffset NextAttemptAt { get; set; } = DateTimeOffset.UtcNow;
    public string? LastError { get; set; }
    public DateTimeOffset? SentAt { get; set; }
}
