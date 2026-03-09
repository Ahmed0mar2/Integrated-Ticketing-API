using GP.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace GP.Application.Services;

public class FileService : IFileService
{
    private readonly IWebHostEnvironment _webHostEnvironment;

    public FileService(IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task<string> UploadFileAsync(
    IFormFile file,
    string folderName,
    string[]? allowedExtensions = null,
    CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length ==0)
            throw new ArgumentException("File is empty or null.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (allowedExtensions != null && allowedExtensions.Length >0 && !allowedExtensions.Contains(extension))
            throw new ArgumentException($"Invalid file extension. Allowed extensions are: {string.Join(", ", allowedExtensions)}");

        var fileName = $"{Guid.NewGuid()}{extension}";

        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath ?? string.Empty, folderName);
        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

        var filePath = Path.Combine(uploadsFolder, fileName);

        await using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream, cancellationToken);
        }

        return $"{folderName}/{fileName}";
    }

    public void DeleteFile(string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl)) return;

        var relativePath = fileUrl.TrimStart('/');
        var physicalPath = Path.Combine(_webHostEnvironment.WebRootPath ?? string.Empty, relativePath.Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(physicalPath)) File.Delete(physicalPath);
    }
}
