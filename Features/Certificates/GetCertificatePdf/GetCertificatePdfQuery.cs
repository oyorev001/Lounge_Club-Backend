namespace LoungeClubsApi.Features.Certificates.GetCertificatePdf;

public record GetCertificatePdfQuery(Guid Id) : IQuery<CertificatePdfResult?>;
