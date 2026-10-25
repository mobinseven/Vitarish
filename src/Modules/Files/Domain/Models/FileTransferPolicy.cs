using System.Text;

namespace Vitarish.Files.Domain.Models;

/// <summary>
/// The Files module's byte-transfer ceiling and the formats it accepts for new uploads. Callers cannot choose them.
/// </summary>
public static class FileTransferPolicy
{
    public const long MaximumContentBytes = 512L * 1024L * 1024L;
    public const long MaximumMultipartRequestBytes = MaximumContentBytes + 128L * 1024L;

    /// <summary>The longest file name kept with a file and reported in its transfer records.</summary>
    public const int MaximumFileNameLength = 255;

    /// <summary>The number of leading content bytes <see cref="MatchesSignature"/> needs.</summary>
    public const int SignatureProbeBytes = 16;

    public static readonly IReadOnlyList<FileUploadFormat> UploadFormats =
    [
        new("image/jpeg", [".jpg", ".jpeg"], FileSignature.Jpeg),
        new("image/png", [".png"], FileSignature.Png),
        new("image/webp", [".webp"], FileSignature.WebP),
        new("image/heic", [".heic"], FileSignature.HeifImage),
        new("image/heif", [".heif"], FileSignature.HeifImage),
        new("video/mp4", [".mp4", ".m4v"], FileSignature.Mp4),
        new("video/quicktime", [".mov"], FileSignature.QuickTime),
        new("video/webm", [".webm"], FileSignature.WebM),
        new("application/pdf", [".pdf"], FileSignature.Pdf),
        new("application/vnd.openxmlformats-officedocument.wordprocessingml.document", [".docx"], FileSignature.OfficeOpenXml),
        new("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", [".xlsx"], FileSignature.OfficeOpenXml)
    ];

    private static readonly string[] HeifBrands = ["heic", "heix", "hevc", "hevx", "heim", "heis", "mif1", "msf1"];
    private static readonly string[] QuickTimeAtoms = ["moov", "mdat", "wide", "free", "skip", "pnot"];

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

    /// <summary>
    /// Resolves the upload format for a normalized media type, requiring the file name's extension to belong to it.
    /// </summary>
    public static bool TryResolveUploadFormat(string mediaType, string? fileName, out FileUploadFormat format)
    {
        string extension = Path.GetExtension(fileName ?? string.Empty);
        format = UploadFormats.FirstOrDefault(candidate =>
            string.Equals(candidate.MediaType, mediaType, StringComparison.Ordinal) &&
            candidate.Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase))!;
        return format is not null;
    }

    /// <summary>
    /// Whether the content's leading bytes have the structure of the format, so a renamed file of another kind is rejected.
    /// </summary>
    public static bool MatchesSignature(FileUploadFormat format, ReadOnlySpan<byte> header)
    {
        ArgumentNullException.ThrowIfNull(format);
        return format.Signature switch
        {
            FileSignature.Jpeg => header.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF]),
            FileSignature.Png => header.StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]),
            FileSignature.WebP => Ascii(header, 0, 4) == "RIFF" && Ascii(header, 8, 4) == "WEBP",
            FileSignature.HeifImage => IsoBaseMediaBrand(header) is { } brand && HeifBrands.Contains(brand),
            FileSignature.Mp4 => IsoBaseMediaBrand(header) is { } brand && !HeifBrands.Contains(brand),
            FileSignature.QuickTime => (IsoBaseMediaBrand(header) is { } brand && !HeifBrands.Contains(brand)) ||
                                       QuickTimeAtoms.Contains(Ascii(header, 4, 4)),
            FileSignature.WebM => header.StartsWith((ReadOnlySpan<byte>)[0x1A, 0x45, 0xDF, 0xA3]),
            FileSignature.Pdf => Ascii(header, 0, 5) == "%PDF-",
            FileSignature.OfficeOpenXml => header.StartsWith((ReadOnlySpan<byte>)[0x50, 0x4B, 0x03, 0x04]),
            _ => false
        };
    }

    /// <summary>
    /// The name to keep and report for a client-supplied file name: its last path segment without control or
    /// bidirectional-override characters, bounded in length, or <see langword="null"/> when nothing usable remains.
    /// </summary>
    public static string? NormalizeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        string lastSegment = fileName[(fileName.LastIndexOfAny(['/', '\\']) + 1)..];
        string cleaned = new string(lastSegment.Where(value => !char.IsControl(value) && !IsBidirectionalOverride(value)).ToArray()).Trim();
        if (cleaned.Length > MaximumFileNameLength)
            cleaned = cleaned[..(char.IsHighSurrogate(cleaned[MaximumFileNameLength - 1]) ? MaximumFileNameLength - 1 : MaximumFileNameLength)];
        return cleaned.Length == 0 ? null : cleaned;
    }

    // Embedding, override and isolate controls can make a name display differently from how it is stored.
    private static bool IsBidirectionalOverride(char value) => value is (>= '\u202A' and <= '\u202E') or (>= '\u2066' and <= '\u2069');

    private static string? IsoBaseMediaBrand(ReadOnlySpan<byte> header) =>
        Ascii(header, 4, 4) == "ftyp" ? Ascii(header, 8, 4) : null;

    private static string? Ascii(ReadOnlySpan<byte> header, int offset, int length) =>
        header.Length >= offset + length ? Encoding.ASCII.GetString(header.Slice(offset, length)) : null;
}
