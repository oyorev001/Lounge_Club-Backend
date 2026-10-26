namespace LoungeClubsApi.Features.Payments.GetPaymentStatus;

public record GetPaymentStatusQuery([Required] string Reference) : IQuery<PaymentStatusResponse?>;
