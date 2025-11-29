namespace Vitarish.Files.Core.Domain.Models;

/// <summary>
/// Metadata for a file including extension and size.
/// </summary>
public class FileMetadata
{
    /// <summary>
    /// Gets or sets the file extension (e.g., ".jpg", ".pdf").
    /// </summary>
    public string? Extension { get; set; }

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    public long? Size { get; set; }

    /// <summary>
    /// Gets or sets the original file name.
    /// </summary>
    public string? OriginalFileName { get; set; }

    /// <summary>
    /// Gets or sets additional custom metadata.
    /// </summary>
    public Dictionary<string, string>? CustomMetadata { get; set; }
}
