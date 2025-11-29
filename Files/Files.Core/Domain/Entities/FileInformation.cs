namespace Vitarish.Files.Core.Domain.Entities;

using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vitarish.Base.Domain.Entities;
using Vitarish.Files.Core.Domain.Models;

/// <summary>
/// Represents file information stored in the system.
/// </summary>
public class FileInformation : Entity
{
    /// <summary>
    /// Gets or sets allowed CORS origins for this file.
    /// </summary>
    public string AllowedOrigins { get; set; } = "*";

    /// <summary>
    /// Gets or sets the content type (MIME type) of the file.
    /// </summary>
    public required string ContentType { get; set; }

    /// <summary>
    /// Gets or sets the thumbnail URL for the file.
    /// </summary>
    public string? Thumbnail { get; set; }

    /// <summary>
    /// Gets or sets the file extension (computed from metadata).
    /// </summary>
    public string Extension { get; private set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file size in bytes (computed from metadata).
    /// </summary>
    public long Size { get; private set; }

    /// <summary>
    /// Gets or sets the checksum of the file for integrity verification.
    /// </summary>
    public string? Checksum { get; set; }

    /// <summary>
    /// Gets or sets the URL where the file can be accessed.
    /// </summary>
    public string? Url { get; set; }

    private string? metadata;

    /// <summary>
    /// Gets or sets the serialized metadata JSON string.
    /// </summary>
    public string? Metadata
    {
        get => metadata;
        set
        {
            metadata = value;
            ApplyMetadata(metadata);
        }
    }

    /// <summary>
    /// Gets or sets the tenant/owner ID for multi-tenancy.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the owner alias (maps to TenantId).
    /// </summary>
    [NotMapped]
    public string? Owner
    {
        get => TenantId;
        set => TenantId = value;
    }

    /// <summary>
    /// Gets or sets a tag for categorizing the file.
    /// </summary>
    public string? Tag { get; set; }

    private static readonly JsonSerializerOptions MetadataSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private void ApplyMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            Extension = string.Empty;
            Size = 0;
            return;
        }

        try
        {
            FileMetadata? metadataModel = JsonSerializer.Deserialize<FileMetadata>(metadataJson, MetadataSerializerOptions);
            Extension = metadataModel?.Extension ?? string.Empty;
            Size = metadataModel?.Size ?? 0;
        }
        catch (JsonException)
        {
            Extension = string.Empty;
            Size = 0;
        }
    }
}
