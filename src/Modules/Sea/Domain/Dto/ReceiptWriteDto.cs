namespace Vitarish.Sea.Domain.Dto;

public record ReceiptWriteDto(
    [property: JsonPropertyName("objectId")] Guid ObjectId,
    [property: JsonPropertyName("readerId")] string ReaderId) : EntityWriteDto;
