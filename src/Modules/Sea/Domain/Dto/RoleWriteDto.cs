using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Sea.Domain.Dto;

public record RoleWriteDto(
    [property: JsonPropertyName("name")] string Name) : EntityWriteDto;
