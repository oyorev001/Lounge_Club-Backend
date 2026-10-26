using LoungeClubsApi.Data;
using LoungeClubsApi.Entities;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Helpers;
using LoungeClubsApi.Interfaces;
using LoungeClubsApi.Services;

namespace LoungeClubsApi.Features.Levies.RecordLevyPayment;

/// <summary>
/// Finance/Admin records a payment received outside the gateway
/// (bank deposit, cash at ministry desk). Two-person rule: the record goes to
/// AwaitingConfirmation and a DIFFERENT authorized staff member must confirm
/// it via /payments/{id}/confirm before the levy settles and a cert issues.
/// </summary>
public class RecordLevyPaymentCommandHandler(
    AppDbContext db,
    JurisdictionService jurisdiction,
    ICurrentUserService currentUser)
    : ICommandHandler<RecordLevyPaymentCommand, RecordLevyPaymentResponse>
{
    public async Task<RecordLevyPaymentResponse> Handle(RecordLevyPaymentCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("You must be logged in.");

        var assessment = await db.LevyAssessments
            .Include(l => l.Establishment)
            .FirstOrDefaultAsync(l => l.Id == request.LevyId, cancellationToken)
            ?? throw new KeyNotFoundException("Levy assessment not found.");
        await jurisdiction.EnsureCanActOnAsync(assessment.Establishment.Lga, cancellationToken);

        if (assessment.Status is not (LevyStatus.Pending or LevyStatus.Overdue))
            throw new InvalidOperationException($"Levy {assessment.Reference} is already {assessment.Status}.");

        // One manual record at a time — two staff can both record otherwise,
        // and both could be confirmed into double revenue.
        var existingManual = await db.Transactions
            .AnyAsync(t => t.Context == TransactionContext.Levy
                && t.ContextId == assessment.Id
                && (t.Status == PaymentStatus.AwaitingConfirmation || t.Status == PaymentStatus.Successful),
                cancellationToken);
        if (existingManual)
            throw new InvalidOperationException(
                $"A payment for levy {assessment.Reference} is already recorded or awaiting confirmation.");

        // Cancel any open gateway checkout so a completed Paystack payment can't
        // settle a transaction that has been superseded by the manual record.
        var openCheckouts = await db.Transactions
            .Where(t => t.Context == TransactionContext.Levy
                && t.ContextId == assessment.Id
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
            Amount = assessment.Amount,
            Context = TransactionContext.Levy,
            ContextId = assessment.Id,
            EstablishmentId = assessment.EstablishmentId,
            Status = PaymentStatus.AwaitingConfirmation,
            Channel = PaymentChannel.Manual,
            Processor = "Manual",
            PaidAt = DateTimeOffset.UtcNow,
            RecordedById = userId,
            ExternalReference = request.ExternalReference?.Trim(),
            Narration = $"Manual payment recorded by staff{FormatNote(request.Note)}"
        };
        db.Transactions.Add(transaction);

        await db.SaveChangesAsync(cancellationToken);

        return new RecordLevyPaymentResponse(
            assessment.Id, assessment.Status.ToString(), transaction.Reference);
    }

    private static string FormatNote(string? note)
        => string.IsNullOrWhiteSpace(note) ? "" : $" — {note.Trim()}";
}
