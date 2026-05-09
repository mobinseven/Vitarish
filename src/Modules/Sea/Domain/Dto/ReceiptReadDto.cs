namespace Vitarish.Sea.Domain.Dto;

public record ReceiptReadDto : EntityReadDto
{
    [JsonPropertyName("objectId")]
    public Guid ObjectId { get; set; }

    [JsonPropertyName("readerId")]
    public string ReaderId { get; set; } = string.Empty;
}
