namespace Vorn.Files.Host.Infrastructure.Extensions;

using Vorn.Files.Host.Domain.Options;

public static class ObjectStorageExtensions
{
    public static string BuildObjectKey(this FilesOptions options, string fileName)
    {
        string sanitizedName = fileName.TrimStart('/');
        string? uploadPath = options.UploadPath?.Trim('/');
        return string.IsNullOrEmpty(uploadPath)
            ? sanitizedName
            : string.Join('/', uploadPath, sanitizedName);
    }
}
