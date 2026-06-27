using System.Text.Json.Serialization;

namespace Vitarish.Entities.Contracts.Models;
/// <summary>
/// DTO returned by read operations. Includes audit metadata populated
/// by the entity interceptor on the server.
/// </summary>
public abstract record EntityReadDto : EntityWriteDto
{
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("createdBy")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CreatedBy { get; set; }

    [JsonPropertyName("updatedAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? UpdatedAt { get; set; }

    [JsonPropertyName("updatedBy")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UpdatedBy { get; set; }

    [JsonPropertyName("deletedAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? DeletedAt { get; set; }

    [JsonPropertyName("deletedBy")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DeletedBy { get; set; }

    public bool IsDeleted => DeletedAt is not null;
}
