namespace Vitarish.Sea.Contracts.Responses;

public record ReceiptWriteDto(
    [property: JsonPropertyName("objectId")] Guid ObjectId) : EntityWriteDto;
