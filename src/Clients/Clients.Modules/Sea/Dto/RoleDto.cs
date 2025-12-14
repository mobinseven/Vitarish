namespace Vitarish.Clients.Modules.Sea.Dto;

public record RoleDto(
    [property: JsonPropertyName("name")] string Name) : EntityDto;
