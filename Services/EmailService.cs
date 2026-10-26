using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using LoungeClubsApi.Attributes;
using LoungeClubsApi.Configuration;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Services;

[TransientService]
public class EmailService(IOptions<MailSettings> mailSettings) : IEmailService
{
    private readonly MailSettings _mailSettings = mailSettings.Value;

    public async Task SendEmailAsync(string to, string subject, string htmlMessage, IEnumerable<EmailAttachment>? attachments = null)
    {
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(_mailSettings.DisplayName ?? "Support", _mailSettings.Mail ?? "no-reply@example.com"));
        email.To.Add(MailboxAddress.Parse(to));
        email.Subject = subject;
        var builder = new BodyBuilder
        {
            HtmlBody = htmlMessage
        };
        if (attachments is not null)
        {
            foreach (var a in attachments)
            {
                var bytes = Convert.FromBase64String(a.ContentBase64);
                if (a.InlineContentId is { Length: > 0 } cid)
                {
                    var linked = builder.LinkedResources.Add(a.FileName, bytes, ContentType.Parse(a.ContentType));
                    linked.ContentId = cid;
                    linked.IsAttachment = false;
                }
                else
                {
                    builder.Attachments.Add(a.FileName, bytes, ContentType.Parse(a.ContentType));
                }
            }
        }
        email.Body = builder.ToMessageBody();
        using var smtp = new SmtpClient { Timeout = 30_000 };
        await smtp.ConnectAsync(_mailSettings.Host ?? "localhost", _mailSettings.Port, MailKit.Security.SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_mailSettings.Mail ?? "", _mailSettings.Password ?? "");
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }

    public async Task SendPasswordResetEmailAsync(string to, string resetLink)
    {
        string subject = "Reset your password";
        string htmlMessage = $@"
            <html>
                <body>
                    <h2>Reset Password</h2>
                    <p>Please click the link below to reset your password:</p>
                    <a href='{resetLink}'>Reset Password</a>
                    <p>If you did not request this, please ignore this email.</p>
                </body>
            </html>";

        await SendEmailAsync(to, subject, htmlMessage);
    }
}
