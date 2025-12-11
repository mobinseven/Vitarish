
using Vitarish.Files.Domain.Options;

namespace Vitarish.Files.Infrastructure.Extensions;

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
