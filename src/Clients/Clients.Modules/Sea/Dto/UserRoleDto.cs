namespace Vitarish.Clients.Modules.Sea.Dto;

public record UserRoleDto(
    [property: JsonPropertyName("roleId")] Guid RoleId,
    [property: JsonPropertyName("type")] RoleType Type) : EntityDto;
