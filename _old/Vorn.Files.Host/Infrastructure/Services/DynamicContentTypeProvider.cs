using Microsoft.AspNetCore.StaticFiles;
using System.IO;

namespace Vorn.Files.Host.Infrastructure.Services;

public class DynamicContentTypeProvider : IContentTypeProvider
{
    public bool TryGetContentType(string subpath, out string contentType)
    {
        if (string.IsNullOrWhiteSpace(subpath))
        {
            contentType = "application/octet-stream";
            return false;
        }


        // Get the file extension from the subpath (e.g., ".png", ".json").
        var extension = Path.GetExtension(subpath);
        if (!string.IsNullOrEmpty(extension))
        {
            extension = extension.ToLowerInvariant();
        }


        // Use your extension map first (assumes it returns null/empty if unknown).
        contentType = ContentTypeExtensions.SafeGetContentType(extension ?? string.Empty);


        if (string.IsNullOrEmpty(contentType))
        {
            // Fallback to ASP.NET Core's built-in provider if your map doesn't know the type.
            var provider = new FileExtensionContentTypeProvider();
            if (provider.TryGetContentType(subpath, out var detected))
            {
                contentType = detected;
                return true;
            }


            // Final fallback: generic binary stream.
            contentType = "application/octet-stream";
            return false;
        }


        return true;
    }
}