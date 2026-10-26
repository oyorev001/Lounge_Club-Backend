namespace LoungeClubsApi.Features.Payments.GetReceiptPdf;

public record GetReceiptPdfQuery(Guid Id) : IQuery<ReceiptPdfResult?>;
