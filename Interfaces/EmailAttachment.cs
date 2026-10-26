namespace LoungeClubsApi.Interfaces;

/// <summary>
/// An email attachment. When InlineContentId is set the part is a linked
/// resource referenced from the HTML body via cid:{InlineContentId} (used
/// for QR codes embedded in the message body).
/// </summary>
public record EmailAttachment(
    string FileName,
    string ContentType,
    string ContentBase64,
    string? InlineContentId = null);
