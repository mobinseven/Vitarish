namespace Vitarish.Sea.Domain.Dto;

public record ReceiptReadDto(
    [property: JsonPropertyName("objectId")] Guid ObjectId) : EntityDto;
