namespace LoungeClubsApi.Features.Certificates.RevokeCertificate;

public record RevokeCertificateCommand(
    Guid Id,
    string Reason
) : ICommand;
