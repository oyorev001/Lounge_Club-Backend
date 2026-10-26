namespace LoungeClubsApi.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string htmlMessage, IEnumerable<EmailAttachment>? attachments = null);
    Task SendPasswordResetEmailAsync(string to, string resetLink);
}
