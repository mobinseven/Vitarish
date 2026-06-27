using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Sea.Contracts.Responses;

public record ReceiptWriteDto(
    [property: JsonPropertyName("objectId")] Guid ObjectId) : EntityWriteDto;
