using Crm.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Crm.Infrastructure.Files;

public sealed class LocalDiskFileStorage : IFileStorage
{
    private readonly string _root;
    public static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".docx", ".xlsx", ".pptx", ".vsdx", ".png", ".jpg", ".jpeg", ".zip", ".msg"
    };
    public const long MaxBytes = 25 * 1024 * 1024;

    public LocalDiskFileStorage(IConfiguration config)
    {
        _root = config["Storage:Root"] ?? Path.Combine(AppContext.BaseDirectory, "storage");
        Directory.CreateDirectory(_root);
    }

    public async Task<StoredFile> SaveAsync(Stream content, string originalFileName, string contentType, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(originalFileName);
        if (!AllowedExtensions.Contains(ext))
            throw new Application.Common.BusinessRuleException($"File type '{ext}' is not allowed.");

        var now = DateTime.UtcNow;
        var relativeDir = Path.Combine(now.Year.ToString("0000"), now.Month.ToString("00"));
        Directory.CreateDirectory(Path.Combine(_root, relativeDir));
        var stored = Path.Combine(relativeDir, $"{Guid.NewGuid():N}{ext}").Replace('\\', '/');
        var full = Path.Combine(_root, stored);

        await using var fs = File.Create(full);
        await content.CopyToAsync(fs, ct);
        var size = fs.Length;
        if (size > MaxBytes)
        {
            fs.Close();
            File.Delete(full);
            throw new Application.Common.BusinessRuleException("File exceeds the 25 MB limit.");
        }

        return new StoredFile(stored, Path.GetFileName(originalFileName), contentType, size);
    }

    public Task<Stream> OpenReadAsync(string storedFileName, CancellationToken ct = default)
    {
        var full = Path.Combine(_root, storedFileName.Replace('/', Path.DirectorySeparatorChar));
        Stream stream = File.OpenRead(full);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storedFileName, CancellationToken ct = default)
    {
        var full = Path.Combine(_root, storedFileName.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(full)) File.Delete(full);
        return Task.CompletedTask;
    }
}
