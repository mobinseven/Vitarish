namespace Vitarish.Sea.Domain.Dto;

public record UserRoleDto(
    [property: JsonPropertyName("roleId")] Guid RoleId,
    [property: JsonPropertyName("type")] RoleType Type) : EntityDto;
