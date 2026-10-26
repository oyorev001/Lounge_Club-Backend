using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Interfaces;
using LoungeClubsApi.Services;

namespace LoungeClubsApi.Features.Payments.GetPaymentStatus;

public class GetPaymentStatusQueryHandler(
    AppDbContext db,
    ICurrentUserService currentUser,
    IHttpContextAccessor httpContextAccessor,
    IPaymentGateway paymentGateway,
    PaymentSettlementService settlement,
    CertificateIssuanceService certificateIssuance,
    ILogger<GetPaymentStatusQueryHandler> logger)
    : IQueryHandler<GetPaymentStatusQuery, PaymentStatusResponse?>
{
    public async Task<PaymentStatusResponse?> Handle(GetPaymentStatusQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("You must be logged in.");

        var transaction = await db.Transactions
            .Include(t => t.Establishment)
            .FirstOrDefaultAsync(t => t.Reference == query.Reference, cancellationToken);

        if (transaction is null)
            return null;

        if (transaction.Establishment?.OwnerId != userId && !IsStaff())
            throw new UnauthorizedAccessException("You do not have access to this transaction.");

        // Verify-on-poll: if the gateway reports success but the webhook never
        // arrived (common in practice), settle the payment right here.
        if (transaction.Status is PaymentStatus.Pending or PaymentStatus.Failed or PaymentStatus.Cancelled
            && transaction.Processor == "Paystack")
        {
            var result = await paymentGateway.VerifyPaymentAsync(transaction.Reference, cancellationToken);
            if (result is { Succeeded: true })
            {
                var expectedKobo = (long)Math.Round(transaction.Amount * 100m, MidpointRounding.AwayFromZero);
                if (result.AmountKobo >= expectedKobo)
                {
                    await settlement.SettleSuccessfulAsync(
                        transaction,
                        result.GatewayReference,
                        result.Channel,
                        payerName: null,
                        result.PaidAt,
                        cancellationToken);
                }
                else
                {
                    // Real money but the wrong amount — fail it like the webhook
                    // so the checkout index frees and the owner can retry.
                    transaction.Status = PaymentStatus.Failed;
                    transaction.FailureReason = $"Amount paid is less than the required ₦{transaction.Amount:N2}.";
                }
            }
        }

        // Self-healing: the transaction settled but certificate issuance failed
        // afterwards (e.g. transient DB error) — retry issuance on poll so a
        // paid levy can never be stranded without a certificate.
        if (transaction.Status == PaymentStatus.Successful
            && transaction.Context == TransactionContext.Levy
            && transaction.ContextId is not null
            && !await db.Certificates.AnyAsync(c => c.LevyAssessmentId == transaction.ContextId.Value, cancellationToken))
        {
            try
            {
                await certificateIssuance.IssueForPaidLevyAsync(transaction, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to re-issue certificate for settled transaction {Ref}", transaction.Reference);
            }
        }

        // Same healing for fines: a settled transaction whose fine was never
        // marked paid (settlement crashed after the claim) gets retried here.
        if (transaction.Status == PaymentStatus.Successful
            && transaction.Context == TransactionContext.Fine
            && transaction.ContextId is not null
            && await db.Fines.AnyAsync(f => f.Id == transaction.ContextId.Value && f.Status == FineStatus.Unpaid, cancellationToken))
        {
            try
            {
                await certificateIssuance.MarkFinePaidAsync(transaction, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to re-settle fine for transaction {Ref}", transaction.Reference);
            }
        }

        Guid? certificateId = null;
        string? certificateNumber = null;

        if (transaction.Context == TransactionContext.Levy && transaction.ContextId is not null)
        {
            var certificate = await db.Certificates
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.LevyAssessmentId == transaction.ContextId.Value, cancellationToken);

            certificateId = certificate?.Id;
            certificateNumber = certificate?.CertificateNumber;
        }

        return new PaymentStatusResponse(
            transaction.Reference,
            transaction.Status,
            transaction.Amount,
            transaction.Context,
            transaction.PaidAt,
            certificateId,
            certificateNumber,
            transaction.FailureReason);
    }

    private bool IsStaff() => httpContextAccessor.HttpContext?.User is { } user
        && (user.IsInRole(nameof(AppRole.Admin))
            
            || user.IsInRole(nameof(AppRole.Officer))
            
            );
}
