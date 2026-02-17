using System.Text.Json;

namespace Vitarish.Sea.Domain.Entities;

public class Procedure : Entity
{
    public Guid ProjectId { get; set; }
    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        private set => SetField(ref _name, value, nameof(Name));
    }

    private Dictionary<string, object?> _metadata = [];
    public Dictionary<string, object?> Metadata
    {
        get => _metadata;
        private set => SetField(ref _metadata, value, nameof(Metadata));
    }

    public Procedure(Guid projectId, string name, string metadata = "{}")
        : this(projectId, name, DeserializeMetadata(metadata))
    {
    }

    public Procedure(Guid projectId, string name, Dictionary<string, object?>? metadata)
    {
        Id = Guid.NewGuid();
        ProjectId = projectId;
        _name = name;
        _metadata = metadata ?? [];
    }


    public ProcedureStats Stats { get; set; } = new();

    public void Rename(string name)
    {
        Name = name;
    }

    public void SetMetadata(string metadata)
    {
        Metadata = DeserializeMetadata(metadata);
    }

    public void SetMetadata(Dictionary<string, object?> metadata)
    {
        Metadata = metadata;
    }

    public static Dictionary<string, object?> DeserializeMetadata(string metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
        {
            return [];
        }

        return JsonSerializer.Deserialize<Dictionary<string, object?>>(metadata) ?? [];
    }

    public static string SerializeMetadata(Dictionary<string, object?> metadata)
        => JsonSerializer.Serialize(metadata ?? []);

    public string MetadataJson => SerializeMetadata(Metadata);

    public string? GetMetadataValueAsString(string key)
    {
        return Metadata.TryGetValue(key, out object? value)
            ? value?.ToString()
            : null;
    }
}
