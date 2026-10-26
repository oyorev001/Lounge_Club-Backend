using LoungeClubsApi.Data;
using LoungeClubsApi.Entities;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Helpers;
using LoungeClubsApi.Interfaces;
using LoungeClubsApi.Services;

namespace LoungeClubsApi.Features.Fines.RecordFinePayment;

/// <summary>
/// Finance/Admin records a fine paid outside the gateway. Goes to
/// AwaitingConfirmation — a different staff member confirms via
/// /payments/{id}/confirm before the fine is marked paid.
/// </summary>
public class RecordFinePaymentCommandHandler(
    AppDbContext db,
    JurisdictionService jurisdiction,
    ICurrentUserService currentUser)
    : ICommandHandler<RecordFinePaymentCommand, RecordFinePaymentResponse>
{
    public async Task<RecordFinePaymentResponse> Handle(RecordFinePaymentCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("You must be logged in.");

        var fine = await db.Fines
            .Include(f => f.Establishment)
            .FirstOrDefaultAsync(f => f.Id == request.FineId, cancellationToken)
            ?? throw new KeyNotFoundException("Fine not found.");
        await jurisdiction.EnsureCanActOnAsync(fine.Establishment.Lga, cancellationToken);

        if (fine.Status != FineStatus.Unpaid)
            throw new InvalidOperationException($"Fine {fine.Reference} is already {fine.Status}.");

        var existingManual = await db.Transactions
            .AnyAsync(t => t.Context == TransactionContext.Fine
                && t.ContextId == fine.Id
                && (t.Status == PaymentStatus.AwaitingConfirmation || t.Status == PaymentStatus.Successful),
                cancellationToken);
        if (existingManual)
            throw new InvalidOperationException(
                $"A payment for fine {fine.Reference} is already recorded or awaiting confirmation.");

        // Cancel any open gateway checkout so a completed Paystack payment
        // can't double-settle a fine the owner already paid offline.
        var openCheckouts = await db.Transactions
            .Where(t => t.Context == TransactionContext.Fine
                && t.ContextId == fine.Id
                && t.Status == PaymentStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var open in openCheckouts)
        {
            open.Status = PaymentStatus.Cancelled;
            open.FailureReason = "Superseded by manual payment.";
        }

        var transaction = new Transaction
        {
            Reference = ReferenceGenerator.PaymentReference(),
            Amount = fine.Amount,
            Context = TransactionContext.Fine,
            ContextId = fine.Id,
            EstablishmentId = fine.EstablishmentId,
            Status = PaymentStatus.AwaitingConfirmation,
            Channel = PaymentChannel.Manual,
            Processor = "Manual",
            PaidAt = DateTimeOffset.UtcNow,
            RecordedById = userId,
            ExternalReference = request.ExternalReference?.Trim(),
            Narration = string.IsNullOrWhiteSpace(request.Note)
                ? "Manual payment recorded by staff"
                : $"Manual payment recorded by staff — {request.Note.Trim()}"
        };
        db.Transactions.Add(transaction);

        await db.SaveChangesAsync(cancellationToken);

        return new RecordFinePaymentResponse(fine.Id, fine.Status.ToString(), transaction.Reference);
    }
}
