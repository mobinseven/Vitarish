namespace Vitarish.Files.Core.Domain.Models;

using System.Text.Json;
using System.Text.Json.Serialization;
using Vitarish.Base.Domain.Models;

/// <summary>
/// Data transfer object for file information.
/// </summary>
public record FileInformationDto : EntityDto
{
    private static readonly JsonSerializerOptions MetadataSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private FileMetadata? _metadata;
    private string? _metadataJson;

    /// <summary>
    /// Gets or sets allowed CORS origins for this file.
    /// </summary>
    public string AllowedOrigins { get; set; } = "*";

    /// <summary>
    /// Gets or sets the content type (MIME type) of the file.
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the thumbnail URL for the file.
    /// </summary>
    public string? Thumbnail { get; set; }

    /// <summary>
    /// Gets or sets the checksum of the file for integrity verification.
    /// </summary>
    public string? Checksum { get; set; }

    /// <summary>
    /// Gets or sets the URL where the file can be accessed.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the owner/tenant ID of the file.
    /// </summary>
    public string? Owner { get; set; }

    /// <summary>
    /// Gets or sets the serialized metadata JSON string.
    /// </summary>
    [JsonIgnore]
    public string? MetadataJson
    {
        get
        {
            FileMetadata metadata = Metadata;
            _metadataJson ??= JsonSerializer.Serialize(metadata, MetadataSerializerOptions);
            return _metadataJson;
        }
        set
        {
            _metadataJson = value;
            _metadata = null;
        }
    }

    /// <summary>
    /// Gets or sets the file metadata.
    /// </summary>
    [JsonPropertyName("metadata")]
    public FileMetadata Metadata
    {
        get
        {
            if (_metadata is not null)
            {
                return _metadata;
            }

            if (!string.IsNullOrWhiteSpace(_metadataJson))
            {
                _metadata = DeserializeMetadata(_metadataJson);
                return _metadata;
            }

            _metadata = new FileMetadata();
            return _metadata;
        }
        set
        {
            _metadata = value ?? new FileMetadata();
            _metadataJson = null;
        }
    }

    /// <summary>
    /// Gets or sets a tag for categorizing the file.
    /// </summary>
    public string? Tag { get; set; }

    private FileMetadata DeserializeMetadata(string metadataJson)
    {
        try
        {
            FileMetadata? metadata = JsonSerializer.Deserialize<FileMetadata>(metadataJson, MetadataSerializerOptions);
            return metadata ?? new FileMetadata();
        }
        catch (JsonException)
        {
            return new FileMetadata();
        }
    }
}
