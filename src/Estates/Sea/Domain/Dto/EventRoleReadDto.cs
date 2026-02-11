namespace Vitarish.Sea.Domain.Dto;

public record EventRoleReadDto(
    [property: JsonPropertyName("eventId")] Guid EventId,
    [property: JsonPropertyName("roleId")] Guid RoleId,
    [property: JsonPropertyName("type")] RoleType Type) : EntityDto;
