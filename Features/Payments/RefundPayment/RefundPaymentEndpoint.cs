using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Features.Payments.RefundPayment;

/// <summary>Flag a settled transaction for refund review (e.g. double payment).</summary>
public class RefundPaymentEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("payments/{id:guid}/refund", async (
            Guid id,
            [FromBody] RefundRequest request,
            AppDbContext db,
            ICurrentUserService currentUser,
            CancellationToken ct) =>
        {
            if (currentUser.UserId is null)
                return Results.Unauthorized();
            if (string.IsNullOrWhiteSpace(request.Reason))
                return Results.BadRequest(new { error = "A refund reason is required." });

            var transaction = await db.Transactions.FirstOrDefaultAsync(t => t.Id == id, ct);
            if (transaction is null)
                return Results.NotFound(new { error = "Transaction not found." });
            if (transaction.Status != PaymentStatus.Successful)
                return Results.BadRequest(new { error = "Only successful transactions can be marked for refund." });

            transaction.Status = PaymentStatus.Refunded;
            transaction.FailureReason = $"Refund requested: {request.Reason.Trim()}";

            // Roll back the settlement side effects — the refund means the money
            // is going back, so the levy/fine re-opens and the certificate is revoked.
            // Unless ANOTHER successful transaction still covers the same debt
            // (double-payment refund) — then the settlement stays.
            var stillCovered = await db.Transactions.AnyAsync(
                t => t.Context == transaction.Context
                    && t.ContextId == transaction.ContextId
                    && t.Id != transaction.Id
                    && t.Status == PaymentStatus.Successful, ct);

            if (!stillCovered && transaction.Context == TransactionContext.Levy && transaction.ContextId is { } levyId)
            {
                var levy = await db.LevyAssessments.FirstOrDefaultAsync(l => l.Id == levyId, ct);
                if (levy is { Status: LevyStatus.Paid })
                {
                    levy.Status = LevyStatus.Pending;
                    levy.PaidAt = null;

                    var certs = await db.Certificates
                        .Where(c => c.LevyAssessmentId == levyId && c.Status == CertificateStatus.Active)
                        .ToListAsync(ct);
                    foreach (var cert in certs)
                    {
                        cert.Status = CertificateStatus.Revoked;
                        cert.RevokedReason = $"Levy payment refunded ({request.Reason.Trim()}).";
                        cert.RevokedAt = DateTimeOffset.UtcNow;
                    }

                    // Mirror RevokeCertificate: with no other active cert the
                    // venue isn't compliant anymore.
                    var est = await db.Establishments.FirstAsync(e => e.Id == levy.EstablishmentId, ct);
                    var otherActive = await db.Certificates.AnyAsync(
                        c => c.EstablishmentId == est.Id && c.Status == CertificateStatus.Active, ct);
                    if (certs.Count > 0 && !otherActive && est.Status == EstablishmentStatus.Approved)
                    {
                        est.Status = EstablishmentStatus.NonCompliant;
                        est.NonCompliantReason = "UnpaidLevy";
                    }
                }
            }
            else if (!stillCovered && transaction.Context == TransactionContext.Fine && transaction.ContextId is { } fineId)
            {
                var fine = await db.Fines.FirstOrDefaultAsync(f => f.Id == fineId, ct);
                if (fine is { Status: FineStatus.Paid })
                {
                    fine.Status = FineStatus.Unpaid;
                    fine.PaidAt = null;
                }
            }
            await db.SaveChangesAsync(ct);

            return Results.Ok(new { transaction.Id, transaction.Status });
        })
        .HasApiVersion(1)
        .WithName(nameof(RefundPaymentEndpoint))
        .WithTags("Payments")
        .RequireAuthorization()
        .HasAnyRole(AppRole.Admin);
    }

    private record RefundRequest(string? Reason);
}
