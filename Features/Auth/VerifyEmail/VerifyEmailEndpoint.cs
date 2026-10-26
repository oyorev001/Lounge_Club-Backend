using LoungeClubsApi.Data;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Services;
using Microsoft.EntityFrameworkCore;

namespace LoungeClubsApi.Features.Auth.VerifyEmail;

public record VerifyEmailRequest(string Email, string Code);

/// <summary>Confirms an account's email using the 6-digit code sent at signup.</summary>
public class VerifyEmailEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("auth/verify-email", async ([FromBody] VerifyEmailRequest request, AppDbContext db, CancellationToken ct) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct);
            // Uniform 400 — never reveal whether the account exists or how close a guess was.
            if (user is null || user.EmailVerificationCodeHash is null)
                return Results.BadRequest(new { error = "Invalid or expired verification code." });

            if (!EmailVerificationService.Check(user, request.Code))
            {
                // 8 wrong guesses wipe the code — the owner must request a fresh one.
                // Stops offline-feasible brute force even if IP limits are bypassed.
                user.EmailVerificationAttempts++;
                if (user.EmailVerificationAttempts >= 8)
                {
                    user.EmailVerificationCodeHash = null;
                    user.EmailVerificationExpiry = null;
                }
                await db.SaveChangesAsync(ct);
                return Results.BadRequest(new { error = "Invalid or expired verification code." });
            }

            user.EmailConfirmed = true;
            user.EmailVerificationCodeHash = null;
            user.EmailVerificationExpiry = null;
            user.EmailVerificationAttempts = 0;
            await db.SaveChangesAsync(ct);

            return Results.Ok(new { message = "Email verified successfully." });
        })
        .HasApiVersion(1)
        .WithName(nameof(VerifyEmailEndpoint))
        .WithTags("Auth")
        .WithSummary("Verify email address")
        .WithDescription("Confirms the account's email using the 6-digit code sent at registration. Codes expire after 30 minutes.")
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .AllowAnonymous().RequireRateLimiting("auth");
    }
}
