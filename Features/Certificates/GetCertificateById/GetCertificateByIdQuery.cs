namespace LoungeClubsApi.Features.Certificates.GetCertificateById;

public record GetCertificateByIdQuery(Guid Id) : IQuery<CertificateDetailResponse?>;
