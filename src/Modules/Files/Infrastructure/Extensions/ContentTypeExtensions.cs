using HeyRed.Mime;

public static class ContentTypeExtensions
{
    public static string SafeGetExtension(string mimeType)
    {
        try
        {
            string ext = MimeTypesMap.GetExtension(mimeType);
            return string.IsNullOrWhiteSpace(ext) ? "bin" : ext.TrimStart('.');
        }
        catch
        {
            return "bin";
        }
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
