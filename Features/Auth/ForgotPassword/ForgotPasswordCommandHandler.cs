using Microsoft.Extensions.Options;
using LoungeClubsApi.Configuration;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Features.Auth.ForgotPassword;

public class ForgotPasswordCommandHandler(
    UserManager<User> userManager,
    AppDbContext db,
    IOptions<AppSettings> appSettings)
    : ICommandHandler<ForgotPasswordCommand>
{
    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            // Don't reveal that the user does not exist
            return;
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);

        var portalUrl = appSettings.Value.PortalUrl.TrimEnd('/');
        var resetLink = $"{portalUrl}/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(request.Email)}";

        // Queue through the outbox — a direct SMTP call can fail and would
        // 500 the endpoint, leaking that the account exists.
        db.EmailOutbox.Add(new EmailOutbox
        {
            To = request.Email,
            Subject = "Reset your password",
            HtmlBody = $"""
                <html>
                    <body>
                        <h2>Reset Password</h2>
                        <p>Please click the link below to reset your password:</p>
                        <a href='{resetLink}'>Reset Password</a>
                        <p>If you did not request this, please ignore this email.</p>
                    </body>
                </html>
                """
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
