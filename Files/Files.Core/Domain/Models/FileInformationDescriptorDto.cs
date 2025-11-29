namespace Vitarish.Files.Core.Domain.Models;

using Vitarish.Base.Domain.Models;

/// <summary>
/// Lightweight descriptor DTO for file information.
/// </summary>
public sealed record FileInformationDescriptorDto : EntityDescriptorDto
{
    /// <summary>
    /// Gets or sets the URL where the file can be accessed.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the file name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the owner/tenant ID.
    /// </summary>
    public string? Owner { get; set; }

    /// <summary>
    /// Gets or sets the checksum for integrity verification.
    /// </summary>
    public string? Checksum { get; set; }

    /// <summary>
    /// Gets or sets a tag for categorization.
    /// </summary>
    public string? Tag { get; set; }
}
