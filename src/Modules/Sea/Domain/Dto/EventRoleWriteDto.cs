using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Sea.Domain.Dto;

public record EventRoleWriteDto(
    [property: JsonPropertyName("eventId")] Guid EventId,
    [property: JsonPropertyName("roleId")] Guid RoleId,
    [property: JsonPropertyName("type")] RoleType Type) : EntityWriteDto;
