namespace Vitarish.Sea.Domain.Dto;

public record RoleReadDto(
    [property: JsonPropertyName("name")] string Name) : EntityDto;
