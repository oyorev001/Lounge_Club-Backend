using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Configuration;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Helpers;
using LoungeClubsApi.Interfaces;
using Microsoft.Extensions.Options;

namespace LoungeClubsApi.Features.Payments.InitiateLevyPayment;

public class InitiateLevyPaymentCommandHandler(
    AppDbContext db,
    ICurrentUserService currentUser,
    IHttpContextAccessor httpContextAccessor,
    IPaymentGateway paymentGateway,
    IOptions<AppSettings> appSettings)
    : ICommandHandler<InitiateLevyPaymentCommand, InitiateLevyPaymentResponse>
{
    public async Task<InitiateLevyPaymentResponse> Handle(InitiateLevyPaymentCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("You must be logged in.");

        var assessment = await db.LevyAssessments
            .Include(a => a.Establishment)
            .ThenInclude(e => e.Owner)
            .Include(a => a.Tier)
            .FirstOrDefaultAsync(a => a.Id == request.LevyAssessmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Levy assessment not found.");

        var establishment = assessment.Establishment;

        if (establishment.OwnerId != userId && !IsStaff())
            throw new UnauthorizedAccessException("You do not own this establishment.");

        if (assessment.Amount <= 0)
            throw new InvalidOperationException($"Levy {assessment.Reference} has no payable amount.");

        if (assessment.Status is not (LevyStatus.Pending or LevyStatus.Overdue))
            throw new InvalidOperationException(
                $"Levy assessment {assessment.Reference} cannot be paid (status: {assessment.Status}).");

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var idempotent = await db.Transactions
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.IdempotencyKey == request.IdempotencyKey
                    && t.Context == TransactionContext.Levy
                    && t.ContextId == assessment.Id
                    && t.Status == PaymentStatus.Pending, cancellationToken);

            if (idempotent is not null)
                return ToResponse(idempotent);
        }

        var now = DateTimeOffset.UtcNow;

        var pending = await db.Transactions
            .Where(t => t.Context == TransactionContext.Levy
                && t.ContextId == assessment.Id
                && t.Status == PaymentStatus.Pending
                && (t.ExpiresAt == null || t.ExpiresAt > now))
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (pending is not null)
            return ToResponse(pending);

        var transaction = new Transaction
        {
            Reference = ReferenceGenerator.PaymentReference(),
            Amount = assessment.Amount,
            Status = PaymentStatus.Pending,
            Channel = PaymentChannel.BankTransfer,
            Processor = "Paystack",
            Context = TransactionContext.Levy,
            ContextId = assessment.Id,
            EstablishmentId = assessment.EstablishmentId,
            IdempotencyKey = request.IdempotencyKey,
            ExpiresAt = now.AddHours(24)
        };

        var owner = establishment.Owner;
        var payment = await paymentGateway.InitializePaymentAsync(new PaymentInitRequest(
            owner.Email!,
            owner.FirstName ?? establishment.Name,
            owner.LastName ?? "Owner",
            owner.PhoneNumber ?? establishment.ContactPhone,
            transaction.Amount,
            transaction.Reference,
            TransactionContext.Levy,
            assessment.Id,
            assessment.EstablishmentId,
            // Never trust a client-supplied callback URL — always return to the portal.
            $"{appSettings.Value.PortalUrl.TrimEnd('/')}/payment-callback"), cancellationToken);

        // No checkout URL means Paystack init failed (bad config, API down,
        // rejected request). Saving a Pending row anyway would block retries
        // until the timeout job expires it — fail fast instead.
        if (payment is null || string.IsNullOrWhiteSpace(payment.AuthorizationUrl))
            throw new InvalidOperationException("Payment gateway is unavailable. Please try again later.");

        transaction.CheckoutUrl = payment.AuthorizationUrl;
        transaction.AccessCode = payment.AccessCode;
        transaction.GatewayReference = payment.ProcessorReference;

        db.Transactions.Add(transaction);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            // Lost the race to create the pending checkout — return whichever
            // transaction the other request created (idempotent for callers).
            db.Entry(transaction).State = EntityState.Detached;
            var existing = await db.Transactions
                .AsNoTracking()
                .Where(t => t.Context == TransactionContext.Levy
                    && t.ContextId == assessment.Id
                    && t.Status == PaymentStatus.Pending)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
            if (existing is not null)
                return ToResponse(existing);
            throw;
        }

        return ToResponse(transaction);
    }

    private bool IsStaff() => httpContextAccessor.HttpContext?.User is { } user
        && (user.IsInRole(nameof(AppRole.Admin))
            
            || user.IsInRole(nameof(AppRole.Officer))
            
            );

    private static InitiateLevyPaymentResponse ToResponse(Transaction t) => new(
        t.Id,
        t.Reference,
        t.Amount,
        t.Status,
        t.CheckoutUrl,
        t.ExpiresAt);
}
