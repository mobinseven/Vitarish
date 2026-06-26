namespace Vitarish.Sea.Contracts.Responses;

public record ReceiptReadDto : EntityReadDto
{
    [JsonPropertyName("objectId")]
    public Guid ObjectId { get; set; }
}
