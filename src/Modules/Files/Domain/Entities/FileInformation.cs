
using System.ComponentModel.DataAnnotations.Schema;
using Vitarish.Files.Domain.Models;

namespace Vitarish.Files.Domain.Entities;

public class FileInformation : Entity
{
    public string AllowedOrigins { get; set; } = "*";
    public required string ContentType { get; set; }
    public string Thumbnail { get; set; } = string.Empty;
    public string Extension { get; private set; } = string.Empty;
    public long Size { get; private set; }
    public string Checksum { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;

    public string? Metadata
    {
        get;
        set
        {
            field = value;
            ApplyMetadata(field);
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
    public string? FileName { get; set; }

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