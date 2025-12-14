namespace Vitarish.Clients.Modules.Sea.Dto;

public record ReceiptDto(
    [property: JsonPropertyName("objectId")] Guid ObjectId) : EntityDto;
