namespace Vitarish.Sea.Domain.Dto;

public record ReceiptReadDto : EntityReadDto
{
    [JsonPropertyName("objectId")]
    public Guid ObjectId { get; set; }
}
