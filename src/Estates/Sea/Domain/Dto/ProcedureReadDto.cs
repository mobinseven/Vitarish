namespace Vitarish.Sea.Domain.Dto;

public record ProcedureReadDto : EntityDto
{
    [JsonPropertyName("projectId")]
    public Guid ProjectId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("metadata")]
    public string Metadata { get; set; } = "{}";

    [JsonPropertyName("url")]
    public string Url => $"/sea/procedures/{Id}";

    [JsonPropertyName("stats")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StatsReadDto? Stats { get; set; }
}

