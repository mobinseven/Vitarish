namespace Vitarish.Communications.Domain.Dto;

public record ReminderDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("sourceId")]
    public Guid? SourceId { get; init; }

    [JsonPropertyName("kind")]
    public ReminderKind? Kind { get; init; }

    [JsonPropertyName("recipientUserId")]
    public string? RecipientUserId { get; init; }

    [JsonPropertyName("remindFrom")]
    public DateTime? RemindFrom { get; init; }

    [JsonPropertyName("remindTo")]
    public DateTime? RemindTo { get; init; }

    [JsonPropertyName("sentFrom")]
    public DateTime? SentFrom { get; init; }

    [JsonPropertyName("sentTo")]
    public DateTime? SentTo { get; init; }

    [JsonPropertyName("isSent")]
    public bool? IsSent { get; init; }
}
