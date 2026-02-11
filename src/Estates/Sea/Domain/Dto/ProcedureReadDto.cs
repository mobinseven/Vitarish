namespace Vitarish.Sea.Domain.Dto;

public record ProcedureReadDto(
    [property: JsonPropertyName("projectId")] Guid ProjectId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("metadata")] string Metadata = "{}") : EntityDto
{
    [JsonPropertyName("url")]
    public string Url => $"/sea/procedures/{Id}";

    [JsonPropertyName("stats")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StatsReadDto? Stats { get; init; }
}

