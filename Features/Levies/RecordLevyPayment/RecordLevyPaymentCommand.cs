namespace LoungeClubsApi.Features.Levies.RecordLevyPayment;

public record RecordLevyPaymentCommand(Guid LevyId, string? Note, string? ExternalReference) : ICommand<RecordLevyPaymentResponse>;

public record RecordLevyPaymentResponse(Guid LevyId, string Status, string TransactionReference);
