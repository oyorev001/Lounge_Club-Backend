using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Interfaces;
using LoungeClubsApi.Services;

namespace LoungeClubsApi.Features.Payments.GetReceiptPdf;

public class GetReceiptPdfQueryHandler(
    AppDbContext db,
    ICurrentUserService currentUser,
    IHttpContextAccessor httpContextAccessor,
    DocumentService documentService)
    : IQueryHandler<GetReceiptPdfQuery, ReceiptPdfResult?>
{
    public async Task<ReceiptPdfResult?> Handle(GetReceiptPdfQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("You must be logged in.");

        var transaction = await db.Transactions
            .Include(t => t.Establishment)
            .FirstOrDefaultAsync(t => t.Id == query.Id, cancellationToken);

        if (transaction is null)
            return null;

        if (transaction.Establishment?.OwnerId != userId && !IsStaff())
            throw new UnauthorizedAccessException("You do not have access to this transaction.");

        if (transaction.Status != PaymentStatus.Successful)
            throw new InvalidOperationException("A receipt is only available for successful payments.");

        var contextLabel = transaction.Context == TransactionContext.Levy
            ? "Annual Compliance Levy"
            : "Regulatory Fine";

        string contextDetail;

        if (transaction.Context == TransactionContext.Levy && transaction.ContextId is not null)
        {
            var assessment = await db.LevyAssessments
                .Include(a => a.Tier)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == transaction.ContextId.Value, cancellationToken);

            contextDetail = assessment is not null
                ? $"Annual levy {assessment.Year} — {assessment.Tier.Name}"
                : "Annual compliance levy";
        }
        else if (transaction.Context == TransactionContext.Fine && transaction.ContextId is not null)
        {
            var fine = await db.Fines
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == transaction.ContextId.Value, cancellationToken);

            contextDetail = fine is not null
                ? $"Fine {fine.Reference} — {fine.FineType}"
                : "Regulatory fine";
        }
        else
        {
            contextDetail = contextLabel;
        }

        if (transaction.Establishment is null)
            throw new KeyNotFoundException("The establishment for this transaction no longer exists.");

        var content = documentService.GenerateReceiptPdf(
            transaction,
            transaction.Establishment,
            contextLabel,
            contextDetail);

        return new ReceiptPdfResult(content, $"receipt-{transaction.Reference}.pdf");
    }

    private bool IsStaff() => httpContextAccessor.HttpContext?.User is { } user
        && (user.IsInRole(nameof(AppRole.Admin))
            
            || user.IsInRole(nameof(AppRole.Officer))
            
            );
}
