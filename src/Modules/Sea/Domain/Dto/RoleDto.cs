namespace Vitarish.Sea.Domain.Dto;

public record RoleDto(
    [property: JsonPropertyName("name")] string Name) : EntityDto;
