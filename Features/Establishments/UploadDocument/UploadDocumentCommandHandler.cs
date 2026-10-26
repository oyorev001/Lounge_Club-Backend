using System.Security.Claims;
using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Features.Establishments.UploadDocument;

public class UploadDocumentCommandHandler(
    AppDbContext db,
    ICurrentUserService currentUser,
    IFileUploadService fileUploadService,
    IFileStorage fileStorage,
    IHttpContextAccessor httpContextAccessor)
    : ICommandHandler<UploadDocumentCommand, UploadDocumentResponse>
{
    public async Task<UploadDocumentResponse> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("You must be logged in.");

        var establishment = await db.Establishments
            .FirstOrDefaultAsync(e => e.Id == request.EstablishmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Establishment not found.");

        if (establishment.OwnerId != userId && !IsStaff(httpContextAccessor.HttpContext?.User))
            throw new UnauthorizedAccessException("You do not have access to this establishment.");

        if (request.File is null || request.File.Length == 0)
            throw new InvalidOperationException("A file is required.");

        var storageKey = await fileUploadService.UploadFileAsync(request.File, "establishments", cancellationToken);

        var document = new EstablishmentDocument
        {
            EstablishmentId = establishment.Id,
            DocumentType = request.DocumentType,
            FileUrl = storageKey,
            FileName = request.File.FileName,
            UploadedAt = DateTimeOffset.UtcNow
        };

        db.EstablishmentDocuments.Add(document);
        await db.SaveChangesAsync(cancellationToken);

        var accessUrl = await fileStorage.GetAccessUrlAsync(storageKey, cancellationToken);
        return new UploadDocumentResponse(document.Id, accessUrl, document.DocumentType);
    }

    private static bool IsStaff(ClaimsPrincipal? user) => user is not null && (
        user.IsInRole(nameof(AppRole.Officer)) ||
        user.IsInRole(nameof(AppRole.Admin))   );
}
