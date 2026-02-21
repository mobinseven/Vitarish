using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Vitarish.Entities.Server.Models;
/// <summary>
/// Base DTO for write operations (Add/Update/Remove). Audit fields are
/// excluded because the entity interceptor populates them automatically.
/// </summary>
public abstract record EntityWriteDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    public Dictionary<string, object?> Metadata { get; set; } = [];
    public Metadata MetadataValues
    {
        get;
        init => field = new(Metadata);
    }

    public T? GetField<T>([CallerMemberName] string propertyName = null!) => MetadataValues.GetField<T>(Metadata, propertyName);

    public void SetField<T>(T value, [CallerMemberName] string propertyName = null!)
    {
        MetadataValues.SetField(value, propertyName);
        Metadata = MetadataValues.ToDictionary();
    }
}
