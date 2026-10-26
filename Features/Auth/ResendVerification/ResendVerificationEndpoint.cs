using LoungeClubsApi.Data;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Services;
using Microsoft.EntityFrameworkCore;

namespace LoungeClubsApi.Features.Auth.ResendVerification;

public record ResendVerificationRequest(string Email);

/// <summary>Issues a fresh 6-digit verification code.</summary>
public class ResendVerificationEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("auth/resend-verification", async ([FromBody] ResendVerificationRequest request, AppDbContext db, EmailVerificationService verification, CancellationToken ct) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct);
            // Uniform 200 — never reveal whether the account exists. The
            // 60-second cooldown matches the frontend timer and stops the
            // endpoint becoming an email-bombing relay.
            if (user is not null && !user.EmailConfirmed
                && (user.EmailVerificationExpiry is null
                    || user.EmailVerificationExpiry <= DateTimeOffset.UtcNow.AddMinutes(29)))
            {
                await verification.IssueAsync(user, ct);
            }

            return Results.Ok(new { message = "If the account exists, a new verification code is on its way." });
        })
        .HasApiVersion(1)
        .WithName(nameof(ResendVerificationEndpoint))
        .WithTags("Auth")
        .WithSummary("Resend email verification code")
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .AllowAnonymous().RequireRateLimiting("auth");
    }
}
