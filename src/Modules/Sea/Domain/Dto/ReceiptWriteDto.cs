using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Sea.Domain.Dto;

public record ReceiptWriteDto(
    [property: JsonPropertyName("objectId")] Guid ObjectId) : EntityWriteDto;
