using System.Net.Http.Json;
using System.Text.Json.Serialization;
using LoungeClubsApi.Attributes;
using LoungeClubsApi.Configuration;
using LoungeClubsApi.Interfaces;
using Microsoft.Extensions.Options;

namespace LoungeClubsApi.Services;

/// <summary>Termii SMS gateway — POST /api/sms/send.</summary>
[TransientService]
public class TermiiSmsService : ISmsService
{
    private readonly SmsSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;

    public TermiiSmsService(IOptions<SmsSettings> settings, IHttpClientFactory httpClientFactory)
    {
        _settings = settings.Value;
        _httpClientFactory = httpClientFactory;
    }

    public bool IsConfigured => _settings.Enabled && !string.IsNullOrWhiteSpace(_settings.ApiKey);

    public async Task SendAsync(string to, string message, CancellationToken ct = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("SMS provider is not configured.");

        var client = _httpClientFactory.CreateClient(nameof(TermiiSmsService));
        var response = await client.PostAsJsonAsync($"{_settings.BaseUrl.TrimEnd('/')}/api/sms/send", new
        {
            to = NormalizeMsisdn(to),
            from = _settings.SenderId,
            sms = message,
            type = "plain",
            channel = _settings.Channel,
            api_key = _settings.ApiKey
        }, ct);

        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"SMS provider returned {(int)response.StatusCode}: {body[..Math.Min(200, body.Length)]}");
    }

    /// <summary>Normalize Nigerian numbers to MSISDN (0803… → 234803…).</summary>
    public static string NormalizeMsisdn(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("0") && digits.Length == 11) return "234" + digits[1..];
        if (digits.StartsWith("234")) return digits;
        return digits;
    }
}
