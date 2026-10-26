using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Establishments.UploadDocument;

public record UploadDocumentResponse(
    Guid Id,
    string FileUrl,
    DocumentType DocumentType);
