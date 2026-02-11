namespace Vitarish.Sea.Domain.Dto;

public record RoleReadDto : EntityDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;
}
