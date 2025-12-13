namespace Vitarish.Sea.Domain.Dto;

public record ProjectDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("template")]
    public bool Template { get; set; } = false;

    [JsonPropertyName("sortBy")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ProjectSortBy? SortBy { get; set; }

    [JsonPropertyName("descending")]
    public bool Descending { get; set; } = true;

    [JsonPropertyName("roleIds")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<Guid>? RoleIds { get; set; }

    //[JsonPropertyName("includeStats")]
    //public bool IncludeStats { get; set; } = true;
}

