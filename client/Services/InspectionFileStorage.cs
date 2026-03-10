using client.Options;
using Microsoft.Extensions.Options;

namespace client.Services;

public sealed class InspectionFileStorage(IOptions<InspectionStorageOptions> options, IWebHostEnvironment environment)
{
    public async Task<StoredInspectionFile> SaveAsync(
        string originalFileName,
        string contentType,
        long fileSizeBytes,
        Stream source,
        CancellationToken cancellationToken)
    {
        ValidateFile(originalFileName, contentType, fileSizeBytes);

        var uploadRoot = GetUploadRoot();
        Directory.CreateDirectory(uploadRoot);

        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var folder = DateTime.UtcNow.ToString("yyyy/MM/dd");
        var generatedFileName = $"{Guid.NewGuid():N}{extension}";
        var relativePath = Path.Combine(folder, generatedFileName);
        var absolutePath = Path.Combine(uploadRoot, relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

        await using var target = File.Create(absolutePath);
        await source.CopyToAsync(target, cancellationToken);

        return new StoredInspectionFile(relativePath.Replace('\\', '/'), generatedFileName);
    }

    public string GetAbsolutePath(string relativePath) => Path.Combine(GetUploadRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));

    private void ValidateFile(string originalFileName, string contentType, long fileSizeBytes)
    {
        if (fileSizeBytes <= 0)
        {
            throw new InvalidOperationException("A non-empty image file is required.");
        }

        if (fileSizeBytes > options.Value.MaxFileSizeBytes)
        {
            throw new InvalidOperationException($"File exceeds the maximum allowed size of {options.Value.MaxFileSizeBytes} bytes.");
        }

        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension) || !options.Value.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Unsupported file type.");
        }

        if (!string.IsNullOrWhiteSpace(contentType) &&
            !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Unsupported content type.");
        }
    }

    private string GetUploadRoot()
    {
        var configuredRoot = options.Value.UploadRoot;
        return Path.IsPathRooted(configuredRoot)
            ? configuredRoot
            : Path.Combine(environment.ContentRootPath, configuredRoot);
    }
}

public sealed record StoredInspectionFile(string RelativePath, string GeneratedFileName);
