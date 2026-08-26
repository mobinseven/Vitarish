using System.Text.Json;

namespace Vitarish.Sea.Domain.Dto;

public sealed record ProjectObjectReadDto(
    Guid Id,
    string OwnerEntityType,
    Guid OwnerEntityId,
    string ObjectType,
    JsonElement Data,
    Guid ConcurrencyToken,
    DateTime CreatedAt,
    string? CreatedBy,
    DateTime? UpdatedAt,
    string? UpdatedBy,
    DateTime? DeletedAt,
    string? DeletedBy);

public sealed record ProjectObjectWriteDto(
    Guid Id,
    string OwnerEntityType,
    Guid OwnerEntityId,
    string ObjectType,
    JsonElement Data,
    Guid ConcurrencyToken);
