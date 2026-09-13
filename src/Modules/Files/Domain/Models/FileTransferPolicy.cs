namespace Vitarish.Files.Domain.Models;

/// <summary>
/// The Files module's current byte-transfer ceiling. Callers cannot choose it.
/// </summary>
public static class FileTransferPolicy
{
    public const long MaximumContentBytes = 64L * 1024L * 1024L;
    public const long MaximumMultipartRequestBytes = MaximumContentBytes + 128L * 1024L;

    public static bool TryNormalizeContentType(string? value, out string contentType)
    {
        contentType = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            System.Net.Mime.ContentType parsed = new(value);
            if (string.IsNullOrWhiteSpace(parsed.MediaType))
            {
                return false;
            }

            contentType = parsed.MediaType.Trim().ToLowerInvariant();
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
