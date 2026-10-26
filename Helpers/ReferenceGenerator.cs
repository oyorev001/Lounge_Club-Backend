using System.Security.Cryptography;

namespace LoungeClubsApi.Helpers;

public static class ReferenceGenerator
{
    /// <summary>Sequential business reference, e.g. EST-000042.</summary>
    public static string Sequential(string prefix, int sequence) => $"{prefix}-{sequence:D6}";

    /// <summary>Year-scoped sequential reference, e.g. OYS-LC-2026-000123.</summary>
    public static string Yearly(string prefix, int year, int sequence) => $"{prefix}-{year}-{sequence:D6}";

    /// <summary>Random payment reference, e.g. OYSLC-20261001-3F9A2B.</summary>
    public static string PaymentReference()
    {
        var stamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
        var suffix = Convert.ToHexString(RandomNumberGenerator.GetBytes(6));
        return $"OYSLC-{stamp}-{suffix}";
    }
}
