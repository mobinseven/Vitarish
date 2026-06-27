using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Sea.Contracts.Responses;

public record RoleWriteDto(
    [property: JsonPropertyName("name")] string Name) : EntityWriteDto;
