namespace LoungeClubsApi.Configuration;

public class PaystackSettings
{
    /// <summary>Default https://api.paystack.co</summary>
    public string? BaseUrl { get; set; }

    /// <summary>sk_live_… / sk_test_… — also used to verify webhook signatures.</summary>
    public string? SecretKey { get; set; }

    /// <summary>pk_live_… / pk_test_… — for inline checkout if ever embedded client-side.</summary>
    public string? PublicKey { get; set; }
}
