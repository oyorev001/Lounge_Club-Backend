namespace LoungeClubsApi.Features.Certificates.RevokeCertificate;

public class RevokeCertificateRequestValidator : AbstractValidator<RevokeCertificateRequest>
{
    public RevokeCertificateRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
