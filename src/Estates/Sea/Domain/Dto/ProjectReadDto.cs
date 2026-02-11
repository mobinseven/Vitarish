namespace Vitarish.Sea.Domain.Dto;

public record ProjectReadDto : EntityReadDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("description")]
    public string Description { get; set; } = default!;

    [JsonPropertyName("template")]
    public bool Template { get; set; }

    [JsonPropertyName("metadata")]
    public string Metadata { get; set; } = default!;

    [JsonIgnore]
    public object? MetadataObject { get; set; }

    [JsonPropertyName("url")]
    public string Url => $"/sea/projects/{Id}";

    [JsonPropertyName("stats")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StatsReadDto? Stats { get; set; }
}
