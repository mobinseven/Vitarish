namespace Vitarish.Sea.Domain.Dto;

public record UserRoleWriteDto(
    [property: JsonPropertyName("roleId")] Guid RoleId,
    [property: JsonPropertyName("type")] RoleType Type) : EntityWriteDto;
