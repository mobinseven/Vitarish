using HeyRed.Mime;

public static class ContentTypeExtensions
{
    public static bool TryGetExtension(string mimeType, out string extension)
    {
        extension = string.Empty;
        try
        {
            extension = MimeTypesMap.GetExtension(mimeType)?.TrimStart('.') ?? string.Empty;
            return !string.IsNullOrWhiteSpace(extension);
        }
        catch
        {
            return false;
        }
    }

    public static string SafeGetExtension(string mimeType)
    {
        return TryGetExtension(mimeType, out string extension) ? extension : "bin";
    }

    public static string SafeGetContentType(string extension)
    {
        const string fallback = "application/octet-stream";


        if (string.IsNullOrWhiteSpace(extension))
        {
            return fallback;
        }

        try
        {
            // Normalize: trim, remove leading dot, lowercase
            string ext = extension.Trim().TrimStart('.').ToLowerInvariant();
            if (string.IsNullOrEmpty(ext))
            {
                return fallback;
            }


            // MimeTypesMap.GetMimeType can accept a file name or an extension.
            // To be explicit, pass an extension with a leading dot.
            string mime = MimeTypesMap.GetMimeType("." + ext);
            return string.IsNullOrWhiteSpace(mime) ? fallback : mime;
        }
        catch
        {
            return fallback;
        }
    }
}
