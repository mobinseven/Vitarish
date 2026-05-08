namespace Vitarish.Sea.Domain.Dto;

public record ProjectReadDto : EntityReadDto
{
    public string Name { get; set; } = default!;

    public string Description { get; set; } = default!;

    public Guid? AvatarFileId => GetField<Guid?>();

    public bool Template { get; set; }

    public string Url => $"/sea/projects/{Id}";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StatsReadDto? Stats { get; set; }

    public IReadOnlyList<string> UserIds { get; set; } = [];
}
