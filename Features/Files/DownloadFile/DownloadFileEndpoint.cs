using System.Security.Claims;
using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Data;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Features.Files.DownloadFile;

/// <summary>
/// Authorized download of an uploaded establishment document.
/// Files are stored outside wwwroot — this endpoint is the only way in,
/// and it enforces owner-or-staff access on every request.
/// </summary>
public class DownloadFileEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("files/{*key}", HandleAsync)
        .HasApiVersion(1)
        .WithName(nameof(DownloadFileEndpoint))
        .WithTags("Files")
        .WithSummary("Download an uploaded establishment document")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        string key,
        AppDbContext db,
        ICurrentUserService currentUser,
        IFileStorage storage,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Results.Unauthorized();

        var doc = await db.EstablishmentDocuments
            .AsNoTracking()
            .Include(d => d.Establishment)
            .FirstOrDefaultAsync(d => d.FileUrl == key, ct);

        if (doc is null)
            return Results.NotFound(new { error = "File not found." });

        if (doc.Establishment.OwnerId != userId && !IsStaff(httpContext.User))
            return Results.Json(new { error = "You do not have access to this document." },
                statusCode: StatusCodes.Status403Forbidden);

        var stream = await storage.OpenReadAsync(key, ct);
        if (stream is null)
            return Results.NotFound(new { error = "File not found." });

        return Results.File(stream, ContentTypeFor(key), doc.FileName ?? Path.GetFileName(key));
    }

    private static string ContentTypeFor(string key) => Path.GetExtension(key).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".pdf" => "application/pdf",
        _ => "application/octet-stream"
    };

    private static bool IsStaff(ClaimsPrincipal user) =>
        user.IsInRole(nameof(AppRole.Officer)) ||
        user.IsInRole(nameof(AppRole.Admin))   ;
}
