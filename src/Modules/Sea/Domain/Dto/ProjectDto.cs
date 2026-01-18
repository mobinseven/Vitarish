namespace Vitarish.Sea.Domain.Dto;

public record ProjectDto : EntityDto
{
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("description")] public string Description { get; set; }
    [JsonPropertyName("template")] public bool Template { get; set; }
    [JsonPropertyName("metadata")] public string Metadata { get; set; }
    [JsonIgnore] public object? MetadataObject { get; set; }
    [JsonPropertyName("url")] public string Url => $"/sea/projects/{Id}";
    [JsonPropertyName("stats")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public StatsDto? Stats { get; init; }
}
