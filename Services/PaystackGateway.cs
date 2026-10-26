using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using LoungeClubsApi.Configuration;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Services;

/// <summary>
/// Paystack gateway — hosted checkout (card, bank transfer, USSD) via
/// /transaction/initialize, HMAC-SHA512 webhook verification (x-paystack-signature,
/// signed with the secret key), and server-side /transaction/verify for settling
/// payments even when webhooks never arrive.
/// </summary>
public class PaystackGateway(IHttpClientFactory httpClientFactory, IOptions<PaystackSettings> settings) : IPaymentGateway
{
    private readonly PaystackSettings _settings = settings.Value;

    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient("Paystack");
        if (client.BaseAddress is null)
            client.BaseAddress = new Uri(_settings.BaseUrl ?? "https://api.paystack.co");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {_settings.SecretKey}");
        return client;
    }

    private bool Configured => !string.IsNullOrWhiteSpace(_settings.SecretKey);

    public async Task<PaymentInitResult?> InitializePaymentAsync(PaymentInitRequest request, CancellationToken cancellationToken = default)
    {
        if (!Configured)
            return null; // gateway not configured — caller falls back to manual flow

        var client = CreateClient();
        var body = new
        {
            email = request.Email,
            // Paystack amounts are in kobo
            amount = (long)Math.Round(request.AmountNaira * 100m, MidpointRounding.AwayFromZero),
            reference = request.Reference,
            callback_url = request.CallbackUrl,
            channels = new[] { "card", "bank_transfer", "ussd", "bank" },
            metadata = new
            {
                context = request.Context.ToString(),
                context_id = request.ContextId,
                establishment_id = request.EstablishmentId,
                payer_name = $"{request.FirstName} {request.LastName}".Trim(),
                phone = request.Phone,
                custom_fields = Array.Empty<object>()
            }
        };

        var response = await client.PostAsJsonAsync("transaction/initialize", body, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        PaystackEnvelope<InitializeData>? data;
        try
        {
            data = await response.Content.ReadFromJsonAsync<PaystackEnvelope<InitializeData>>(cancellationToken);
        }
        catch (System.Text.Json.JsonException)
        {
            return null; // Paystack returned a non-JSON body (gateway/proxy error)
        }

        if (data?.Data is null || string.IsNullOrWhiteSpace(data.Data.AuthorizationUrl))
            return null;

        return new PaymentInitResult(
            data.Data.AuthorizationUrl,
            data.Data.AccessCode ?? string.Empty,
            data.Data.Reference ?? request.Reference);
    }

    public async Task<PaymentVerifyResult?> VerifyPaymentAsync(string reference, CancellationToken cancellationToken = default)
    {
        if (!Configured)
            return null;

        var client = CreateClient();
        var response = await client.GetAsync($"transaction/verify/{Uri.EscapeDataString(reference)}", cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        PaystackEnvelope<VerifyData>? data;
        try
        {
            data = await response.Content.ReadFromJsonAsync<PaystackEnvelope<VerifyData>>(cancellationToken);
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }

        var tx = data?.Data;
        if (tx is null)
            return null;

        var succeeded = string.Equals(tx.Status, "success", StringComparison.OrdinalIgnoreCase);
        DateTimeOffset? paidAt = null;
        if (!string.IsNullOrWhiteSpace(tx.PaidAt) && DateTimeOffset.TryParse(tx.PaidAt, out var parsed))
            paidAt = parsed;

        return new PaymentVerifyResult(succeeded, tx.Amount, tx.Channel, tx.Id?.ToString(), paidAt);
    }

    public bool VerifyWebhookSignature(string payload, string signature)
    {
        // Paystack signs the raw request body with the secret key (HMAC-SHA512, hex-encoded)
        if (!Configured || string.IsNullOrWhiteSpace(signature))
            return false;

        try
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_settings.SecretKey!));
            var computed = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var expected = Convert.FromHexString(signature.Trim().ToLowerInvariant());
            return CryptographicOperations.FixedTimeEquals(computed, expected);
        }
        catch (FormatException)
        {
            return false; // signature wasn't valid hex
        }
    }

    private class PaystackEnvelope<T>
    {
        [JsonPropertyName("status")]
        public bool Status { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }
    }

    private class InitializeData
    {
        [JsonPropertyName("authorization_url")]
        public string? AuthorizationUrl { get; set; }

        [JsonPropertyName("access_code")]
        public string? AccessCode { get; set; }

        [JsonPropertyName("reference")]
        public string? Reference { get; set; }
    }

    private class VerifyData
    {
        [JsonPropertyName("id")]
        public long? Id { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonPropertyName("channel")]
        public string? Channel { get; set; }

        [JsonPropertyName("paid_at")]
        public string? PaidAt { get; set; }
    }
}
