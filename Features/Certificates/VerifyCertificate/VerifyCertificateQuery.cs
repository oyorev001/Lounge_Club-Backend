namespace LoungeClubsApi.Features.Certificates.VerifyCertificate;

public record VerifyCertificateQuery([Required] string CertOrToken) : IQuery<VerifyCertificateResponse>;
