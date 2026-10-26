using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Establishments.UploadDocument;

public record UploadDocumentCommand(
    Guid EstablishmentId,
    IFormFile File,
    DocumentType DocumentType
) : ICommand<UploadDocumentResponse>;
