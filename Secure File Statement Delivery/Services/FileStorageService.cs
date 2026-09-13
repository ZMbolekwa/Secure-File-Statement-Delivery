using System.Security.Cryptography;

namespace Secure_File_Statement_Delivery.Services;

public class FileStorageService
{
    private readonly IWebHostEnvironment _environment;

    public FileStorageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<(string FilePath, string Hash)> SavePdfAsync(
        IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("A PDF file is required.");
        }

        if (!string.Equals(
                file.ContentType,
                "application/pdf",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Only PDF files are allowed.");
        }

        const long maxFileSize = 10 * 1024 * 1024;

        if (file.Length > maxFileSize)
        {
            throw new ArgumentException(
                "The PDF cannot be larger than 10 MB.");
        }

        var extension = Path.GetExtension(file.FileName);

        if (!string.Equals(
                extension,
                ".pdf",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The file must have a .pdf extension.");
        }

        var folder = Path.Combine(
            _environment.ContentRootPath,
            "Storage",
            "Statements");

        Directory.CreateDirectory(folder);

        var storedFileName =
            $"{Guid.NewGuid():N}.pdf";

        var fullPath =
            Path.Combine(folder, storedFileName);

        await using (var stream =
            new FileStream(fullPath, FileMode.CreateNew))
        {
            await file.CopyToAsync(stream);
        }

        using var sha256 = SHA256.Create();

        await using var hashStream =
            new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read);

        var hash =
            await sha256.ComputeHashAsync(hashStream);

        return (
            fullPath,
            Convert.ToHexString(hash)
        );
    }
}


