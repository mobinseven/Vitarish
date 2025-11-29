namespace Vorn.Files.Core.Domain.Entities;

using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vorn.Entities;
using Vorn.Files.Core.Domain.Models;

public class FileInformation : Entity
{
    public string AllowedOrigins { get; set; } = "*";
    public required string ContentType { get; set; }
    public string Thumbnail { get; set; }
    public string Extension { get; private set; } = string.Empty;
    public long Size { get; private set; }
    public string Checksum { get; set; }
    public string Url { get; set; }
    private string? metadata;
    public string? Metadata
    {
        get => metadata;
        set
        {
            metadata = value;
            ApplyMetadata(metadata);
        }
    }
    public string? TenantId { get; set; }

    [NotMapped]
    public string? Owner
    {
        get => TenantId;
        set => TenantId = value;
    }
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