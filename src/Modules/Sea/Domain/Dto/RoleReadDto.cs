using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Sea.Domain.Dto;

public record RoleReadDto : EntityReadDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;
}
