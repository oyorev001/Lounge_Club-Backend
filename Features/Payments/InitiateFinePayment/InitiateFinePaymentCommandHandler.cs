using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Configuration;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Helpers;
using LoungeClubsApi.Interfaces;
using Microsoft.Extensions.Options;

namespace LoungeClubsApi.Features.Payments.InitiateFinePayment;

public class InitiateFinePaymentCommandHandler(
    AppDbContext db,
    ICurrentUserService currentUser,
    IHttpContextAccessor httpContextAccessor,
    IPaymentGateway paymentGateway,
    IOptions<AppSettings> appSettings)
    : ICommandHandler<InitiateFinePaymentCommand, InitiateFinePaymentResponse>
{
    public async Task<InitiateFinePaymentResponse> Handle(InitiateFinePaymentCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("You must be logged in.");

        var fine = await db.Fines
            .Include(f => f.Establishment)
            .ThenInclude(e => e.Owner)
            .FirstOrDefaultAsync(f => f.Id == request.FineId, cancellationToken)
            ?? throw new KeyNotFoundException("Fine not found.");

        var establishment = fine.Establishment;

        if (establishment.OwnerId != userId && !IsStaff())
            throw new UnauthorizedAccessException("You do not own this establishment.");

        if (fine.Amount <= 0)
            throw new InvalidOperationException($"Fine {fine.Reference} has no payable amount.");

        if (fine.Status != FineStatus.Unpaid)
            throw new InvalidOperationException(
                $"Fine {fine.Reference} cannot be paid (status: {fine.Status}).");

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var idempotent = await db.Transactions
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.IdempotencyKey == request.IdempotencyKey
                    && t.Context == TransactionContext.Fine
                    && t.ContextId == fine.Id
                    && t.Status == PaymentStatus.Pending, cancellationToken);

            if (idempotent is not null)
                return ToResponse(idempotent);
        }

        var now = DateTimeOffset.UtcNow;

        var pending = await db.Transactions
            .Where(t => t.Context == TransactionContext.Fine
                && t.ContextId == fine.Id
                && t.Status == PaymentStatus.Pending
                && (t.ExpiresAt == null || t.ExpiresAt > now))
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (pending is not null)
            return ToResponse(pending);

        var transaction = new Transaction
        {
            Reference = ReferenceGenerator.PaymentReference(),
            Amount = fine.Amount,
            Status = PaymentStatus.Pending,
            Channel = PaymentChannel.BankTransfer,
            Processor = "Paystack",
            Context = TransactionContext.Fine,
            ContextId = fine.Id,
            EstablishmentId = fine.EstablishmentId,
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
            TransactionContext.Fine,
            fine.Id,
            fine.EstablishmentId,
            // Never trust a client-supplied callback URL — always return to the portal.
            $"{appSettings.Value.PortalUrl.TrimEnd('/')}/payment-callback"), cancellationToken);

        // See InitiateLevyPayment — a gateway failure must not leave a dead
        // Pending transaction that blocks retries for 24h.
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
                .Where(t => t.Context == TransactionContext.Fine
                    && t.ContextId == fine.Id
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

    private static InitiateFinePaymentResponse ToResponse(Transaction t) => new(
        t.Id,
        t.Reference,
        t.Amount,
        t.Status,
        t.CheckoutUrl,
        t.ExpiresAt);
}
