namespace LoungeClubsApi.Features.Payments.GetReceiptPdf;

public record ReceiptPdfResult(byte[] Content, string FileName);
