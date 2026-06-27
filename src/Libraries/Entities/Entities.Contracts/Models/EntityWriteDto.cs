using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Vitarish.Entities.Contracts.Models;
/// <summary>
/// Base DTO for write operations (Add/Update/Remove). Audit fields are
/// excluded because the entity interceptor populates them automatically.
/// </summary>
public abstract record EntityWriteDto
{
    private Dictionary<string, object?> _metadata = [];
    private Metadata _metadataValues = [];

    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    public Dictionary<string, object?> Metadata
    {
        get => _metadata;
        set
        {
            _metadata = value is null ? [] : new(value);
            _metadataValues = new(_metadata);
        }
    }

    public Metadata MetadataValues
    {
        get
        {
            SynchronizeMetadataValues();
            return _metadataValues;
        }
        init
        {
            _metadataValues = value is null ? [] : new(value);
            _metadata = _metadataValues.ToDictionary();
        }
    }

    public T? GetField<T>([CallerMemberName] string propertyName = null!)
    {
        SynchronizeMetadataValues();
        return _metadataValues.GetField<T>(_metadata, propertyName);
    }

    public void SetField<T>(T value, [CallerMemberName] string propertyName = null!)
    {
        SynchronizeMetadataValues();
        _metadataValues.SetField(value, propertyName);
        _metadata = _metadataValues.ToDictionary();
    }

    private void SynchronizeMetadataValues()
    {
        if (HasSameMetadata(_metadata, _metadataValues))
        {
            return;
        }

        _metadataValues = new(_metadata);
    }

    private static bool HasSameMetadata(
        IReadOnlyDictionary<string, object?> metadata,
        IReadOnlyDictionary<string, object?> metadataValues)
    {
        if (metadata.Count != metadataValues.Count)
        {
            return false;
        }

        foreach ((string key, object? value) in metadata)
        {
            if (!metadataValues.TryGetValue(key, out object? metadataValue) || !Equals(value, metadataValue))
            {
                return false;
            }
        }

        return true;
    }
}
