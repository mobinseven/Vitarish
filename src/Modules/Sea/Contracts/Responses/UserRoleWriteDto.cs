namespace Vitarish.Sea.Contracts.Responses;

public record UserRoleWriteDto(
    [property: JsonPropertyName("roleId")] Guid RoleId,
    [property: JsonPropertyName("type")] RoleType Type) : EntityWriteDto;
