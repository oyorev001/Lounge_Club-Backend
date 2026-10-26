namespace LoungeClubsApi.Features.Payments.PaystackWebhook;

public record PaystackWebhookCommand(
    string Payload,
    string? Signature
) : ICommand<PaystackWebhookResponse>;
