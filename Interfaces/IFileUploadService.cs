namespace LoungeClubsApi.Interfaces;

public interface IFileUploadService
{
    /// <summary>Uploads a file to wwwroot/uploads/{subfolder}. Returns the relative URL.</summary>
    Task<string> UploadFileAsync(IFormFile file, string subfolder, CancellationToken cancellationToken = default);
}
