namespace LoungeClubsApi.Interfaces;

/// <summary>SMS gateway abstraction — Termii/Africa's Talking/Twilio-compatible.</summary>
public interface ISmsService
{
    Task SendAsync(string to, string message, CancellationToken ct = default);
    bool IsConfigured { get; }
}
