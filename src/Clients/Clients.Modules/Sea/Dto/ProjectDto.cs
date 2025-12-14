namespace Vitarish.Clients.Modules.Sea.Dto;

public record ProjectDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("template")] bool Template,
    [property: JsonPropertyName("metadata")] string Metadata) : EntityDto
{
    [JsonPropertyName("url")]
    public string Url => $"/sea/projects/{Id}";

    [JsonPropertyName("stats")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StatsDto? Stats { get; init; }
}
