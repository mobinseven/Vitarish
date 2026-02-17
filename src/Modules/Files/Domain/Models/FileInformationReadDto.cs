namespace Vitarish.Files.Domain.Models;

using System.Text.Json;
using System.Text.Json.Serialization;
using Vitarish.Entities.Server.Models;

public record FileInformationReadDto : EntityReadDto
{
    private static readonly JsonSerializerOptions MetadataSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private FileMetadata? _metadata;
    private string? _metadataJson;
    public string AllowedOrigins { get; set; } = "*";
    public string ContentType { get; set; } = string.Empty;
    public string? Thumbnail { get; set; }
    public string Checksum { get; set; }
    public string Url { get; set; }
    public string? Owner { get; set; }

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

    [JsonPropertyName("metadata")]
    public FileMetadata Metadata
    {
        get
        {
            if(_metadata is not null)
            {
                return _metadata;
            }

            if(!string.IsNullOrWhiteSpace(_metadataJson))
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

    public string? Tag { get; set; }

    private FileMetadata DeserializeMetadata(string metadataJson)
    {
        try
        {
            FileMetadata? metadata = JsonSerializer.Deserialize<FileMetadata>(metadataJson, MetadataSerializerOptions);

            if(metadata is null)
            {
                return new FileMetadata();
            }

            return metadata;
        }
        catch(JsonException)
        {
            return new FileMetadata();
        }
    }
}
