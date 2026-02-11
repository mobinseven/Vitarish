namespace Vitarish.Sea.Domain.Dto;

public record UserRoleReadDto(
    [property: JsonPropertyName("roleId")] Guid RoleId,
    [property: JsonPropertyName("type")] RoleType Type) : EntityDto;
