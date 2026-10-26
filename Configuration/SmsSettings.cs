using System.ComponentModel.DataAnnotations;

namespace LoungeClubsApi.Configuration;

/// <summary>
/// SMS provider settings — Termii-compatible JSON API.
/// Configure via environment: SmsSettings__ApiKey, __SenderId, __BaseUrl, __Enabled.
/// When disabled, queued SMS rows are skipped (logged once).
/// </summary>
public class SmsSettings
{
    public bool Enabled { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string SenderId { get; set; } = "OYO-LC";
    /// <summary>Termii default; swap for Africa's Talking etc.</summary>
    public string BaseUrl { get; set; } = "https://v3.api.termii.com";
    public string Channel { get; set; } = "generic"; // Termii channels: generic, dnd, whatsapp
}
