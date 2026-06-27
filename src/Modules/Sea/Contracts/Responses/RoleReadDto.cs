using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Sea.Contracts.Responses;

public record RoleReadDto : EntityReadDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;
}
