namespace Vitarish.Sea.Domain.Dto;

public record ReceiptReadDto : EntityDto
{
    [JsonPropertyName("objectId")]
    public Guid ObjectId { get; set; }
}
