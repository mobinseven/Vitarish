namespace Vitarish.Sea.Domain.Dto;

public record ReceiptDto(
    [property: JsonPropertyName("objectId")] Guid ObjectId) : EntityDto;
