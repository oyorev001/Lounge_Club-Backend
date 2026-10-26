namespace LoungeClubsApi.Features.Payments.PaystackWebhook;

public class PaystackWebhookEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("webhooks/paystack", async (HttpRequest request, IMediator mediator, CancellationToken ct) =>
        {
            // Paystack charge events are a few KB — anything larger is not a
            // webhook, so refuse before buffering an unbounded body into memory.
            if (request.ContentLength > 256 * 1024)
                return Results.BadRequest();

            // ReadBlockAsync fills the buffer only when the body overflows it —
            // a full buffer means "too large", a partial one means EOF.
            using var reader = new StreamReader(request.Body);
            var buffer = new char[256 * 1024];
            var read = await reader.ReadBlockAsync(buffer, ct);
            if (read == buffer.Length)
                return Results.BadRequest();

            var payload = new string(buffer, 0, read);
            var signature = request.Headers["x-paystack-signature"].ToString();

            var result = await mediator.SendCommandAsync<PaystackWebhookCommand, PaystackWebhookResponse>(
                new PaystackWebhookCommand(payload, signature), ct);

            return result.SignatureValid ? Results.Ok() : Results.Unauthorized();
        })
        .HasApiVersion(1)
        .WithName(nameof(PaystackWebhookEndpoint))
        .WithTags("Payments")
        .WithSummary("Paystack payment webhook")
        .ExcludeFromDescription()
        .DisableAntiforgery()
        .AllowAnonymous();
    }
}
