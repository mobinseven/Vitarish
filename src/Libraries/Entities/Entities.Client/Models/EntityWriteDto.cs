using System.Text.Json.Serialization;

namespace Vitarish.Entities.Client.Models;

public abstract record EntityWriteDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
}
