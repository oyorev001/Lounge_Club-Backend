using LoungeClubsApi.Entities.Base;
using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Entities;

public class Transaction : AuditableEntity
{
    public string Reference { get; set; } = string.Empty; // OYSLC-yyyyMMddHHmmss-xxxx
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public PaymentChannel Channel { get; set; } = PaymentChannel.BankTransfer;
    public string Processor { get; set; } = "Paystack";
    public TransactionContext Context { get; set; }
    public Guid? ContextId { get; set; } // LevyAssessment.Id or Fine.Id
    public string? IdempotencyKey { get; set; }
    public string? GatewayReference { get; set; }
    public string? WebhookId { get; set; }
    public string? SenderName { get; set; }
    public string? SenderBank { get; set; }
    public string? Narration { get; set; }
    public string? VirtualAccountNumber { get; set; }
    public string? VirtualAccountName { get; set; }
    public string? VirtualAccountBankName { get; set; }
    public string? CheckoutUrl { get; set; } // hosted-checkout URL (Paystack authorization_url)
    public string? AccessCode { get; set; } // Paystack access_code
    public string? FailureReason { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }

    // Manual-payment two-person rule: recorded by one staff member, must be
    // confirmed by a different authorized user before it settles the levy/fine.
    public string? RecordedById { get; set; }
    public string? ConfirmedById { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public string? ExternalReference { get; set; } // bank slip/receipt number for manual payments

    public Guid? EstablishmentId { get; set; }
    public Establishment? Establishment { get; set; }
}
