namespace LoungeClubsApi.Features.Certificates.RevokeCertificate;

public record RevokeCertificateRequest(
    [Required] string Reason);
