using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Establishments.GetEstablishmentById;

public record EstablishmentDocumentItem(
    Guid Id,
    DocumentType DocumentType,
    string FileUrl,
    string? FileName,
    DateTimeOffset UploadedAt);
