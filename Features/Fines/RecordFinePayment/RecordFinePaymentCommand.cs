namespace LoungeClubsApi.Features.Fines.RecordFinePayment;

public record RecordFinePaymentCommand(Guid FineId, string? Note, string? ExternalReference) : ICommand<RecordFinePaymentResponse>;

public record RecordFinePaymentResponse(Guid FineId, string Status, string TransactionReference);
