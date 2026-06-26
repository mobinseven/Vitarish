namespace Vitarish.Sea.Contracts.Responses;

public record ProjectReadDto : EntityReadDto
{
    public string Name { get; set; } = default!;

    public string Description { get; set; } = default!;

    public Guid? AvatarFileId { get; set; }

    public string? IdentificationCode { get; set; }

    public bool Template { get; set; }


    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StatsReadDto? Stats { get; set; }

    public IReadOnlyList<string> UserIds { get; set; } = [];
}
