using Microsoft.AspNetCore.Http;

namespace GP.Application.Interfaces;

public interface IFileService
{
    /// <summary>
    /// Uploads a file to the specified folder inside wwwroot and returns the relative URL.
    /// </summary>
    Task<string> UploadFileAsync(
        IFormFile file,
        string folderName,
        string[]? allowedExtensions = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from the server using its relative URL.
    /// </summary>
    void DeleteFile(string fileUrl);
}