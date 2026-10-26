using LoungeClubsApi.Configuration;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Interfaces;
using Microsoft.Extensions.Options;

namespace LoungeClubsApi.Services;

public class NotificationService(AppDbContext db, IOptions<AppSettings> appSettings, ILogger<NotificationService> logger)
{
    /// <summary>Creates an in-app notification and queues the email (sent async by EmailOutboxJob).</summary>
    public async Task NotifyAsync(
        string userId,
        NotificationType type,
        string title,
        string message,
        string? link = null,
        string? emailTo = null,
        CancellationToken cancellationToken = default,
        string? smsTo = null)
    {
        Stage(userId, type, title, message, link, emailTo, smsTo);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Stages many notifications, then saves once — callers with a
    /// loop (annual levy generation) must use this or pay an N+1 round-trip.</summary>
    public async Task NotifyManyAsync(
        IEnumerable<(string UserId, string Title, string Message, string? Link, string? EmailTo)> items,
        NotificationType type,
        CancellationToken cancellationToken = default)
    {
        foreach (var (userId, title, message, link, emailTo) in items)
            Stage(userId, type, title, message, link, emailTo, null);
        await db.SaveChangesAsync(cancellationToken);
    }

    private void Stage(
        string userId,
        NotificationType type,
        string title,
        string message,
        string? link,
        string? emailTo,
        string? smsTo)
    {
        db.Notifications.Add(new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            Link = link
        });

        if (!string.IsNullOrWhiteSpace(emailTo))
        {
            db.EmailOutbox.Add(new EmailOutbox
            {
                To = emailTo,
                Subject = title,
                HtmlBody = WrapHtml(title, message, appSettings.Value.PortalUrl)
            });
        }

        if (!string.IsNullOrWhiteSpace(smsTo))
        {
            var msisdn = TermiiSmsService.NormalizeMsisdn(smsTo);
            if (msisdn.Length >= 10)
            {
                // SMS body = title + a trimmed copy of the message
                var smsBody = $"{title}: {message}";
                if (smsBody.Length > 459)
                    smsBody = smsBody[..456] + "...";
                db.SmsOutbox.Add(new SmsOutbox { To = msisdn, Message = smsBody });
            }
        }

    }

    private static string WrapHtml(string title, string body, string? portalUrl)
    {
        var crest = string.IsNullOrWhiteSpace(portalUrl)
            ? ""
            : $"""<img src="{portalUrl.TrimEnd('/')}/images/oyo-crest.png" alt="Oyo State crest" width="56" style="display:block;margin:0 auto 10px" />""";
        return $"""
        <div style="font-family:Arial,sans-serif;max-width:600px;margin:auto">
          <div style="background:#700002;padding:24px;text-align:center">
            {crest}
            <h1 style="color:#fff;margin:0;font-size:20px">Oyo State Lounge &amp; Clubs Portal</h1>
            <p style="color:#f5d0c5;margin:6px 0 0;font-size:12px">Ministry of Culture and Tourism — Compliance Directorate</p>
          </div>
          <div style="padding:28px;border:1px solid #ecdcd2">
            <h2 style="color:#450a0a;margin-top:0">{title}</h2>
            <p style="color:#444;line-height:1.6">{body}</p>
          </div>
          <p style="color:#999;font-size:11px;text-align:center;margin-top:16px">
            This is an official communication of the Oyo State Government.<br/>
            &copy; {DateTime.UtcNow.Year} Oyo State Government. All rights reserved.
          </p>
        </div>
        """;
    }
}
