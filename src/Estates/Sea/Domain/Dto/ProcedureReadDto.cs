namespace Vitarish.Sea.Domain.Dto;

public record ProcedureReadDto : EntityReadDto
{
    [JsonPropertyName("projectId")]
    public Guid ProjectId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("url")]
    public string Url => $"/sea/procedures/{Id}";

    [JsonPropertyName("stats")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StatsReadDto? Stats { get; set; }
    public string? Description => GetField<string>();
}

