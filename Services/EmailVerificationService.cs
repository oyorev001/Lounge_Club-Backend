using System.Security.Cryptography;
using System.Text;
using LoungeClubsApi.Attributes;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities;
using LoungeClubsApi.Entities.Enums;
using Microsoft.Extensions.Options;
using LoungeClubsApi.Configuration;

namespace LoungeClubsApi.Services;

/// <summary>
/// Issues 6-digit email-confirmation codes (like a bank OTP) — stored as a
/// SHA-256 hash with a 30-minute expiry, delivered through the email outbox.
/// </summary>
[ScopedService]
public class EmailVerificationService(AppDbContext db, IOptions<AppSettings> appSettings)
{
    public async Task IssueAsync(User user, CancellationToken ct = default)
    {
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        user.EmailVerificationCodeHash = Hash(code, user.Email ?? string.Empty);
        user.EmailVerificationAttempts = 0;
        user.EmailVerificationExpiry = DateTimeOffset.UtcNow.AddMinutes(30);

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            db.EmailOutbox.Add(new EmailOutbox
            {
                To = user.Email,
                Subject = "Confirm your email — Oyo State Lounge & Clubs Portal",
                HtmlBody = WrapHtml(code, appSettings.Value.PortalUrl)
            });
        }

        await db.SaveChangesAsync(ct);
    }

    public static bool Check(User user, string code) =>
        user.EmailVerificationExpiry > DateTimeOffset.UtcNow
        && user.EmailVerificationCodeHash is { Length: > 0 }
        && CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(user.EmailVerificationCodeHash)),
            SHA256.HashData(Encoding.UTF8.GetBytes(Hash(code, user.Email ?? string.Empty))));

    public static string Hash(string code, string email) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{email.Trim().ToLowerInvariant()}|{code.Trim()}")));

    private static string WrapHtml(string code, string? portalUrl)
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
            <h2 style="margin:0 0 8px;font-size:18px;color:#1a1a1a">Confirm your email address</h2>
            <p style="margin:0 0 20px;font-size:14px;color:#555;line-height:1.7">
              Enter this code on the portal to verify your account. It expires in <b>30 minutes</b>.
            </p>
            <div style="background:#faf6f0;border:1px solid #ecdcd2;border-radius:12px;padding:20px;text-align:center;margin-bottom:20px">
              <div style="font-size:34px;font-weight:800;letter-spacing:8px;color:#700002">{code}</div>
            </div>
            <p style="margin:0;font-size:12px;color:#999;text-align:center">
              If you did not create this account, you can safely ignore this email.
            </p>
          </div>
          <div style="background:#faf6f0;border:1px solid #ecdcd2;border-top:0;padding:14px;text-align:center;font-size:11px;color:#999">
            This is an automated message from the Oyo State Government compliance portal — please do not reply.
          </div>
        </div>
        """;
    }
}
