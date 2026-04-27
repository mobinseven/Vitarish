namespace Vitarish.Reminders.Domain.Dto;

public record ReminderDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("sourceId")]
    public Guid? SourceId { get; init; }

    [JsonPropertyName("kind")]
    public ReminderKind? Kind { get; init; }

    [JsonPropertyName("recipientUserId")]
    public string? RecipientUserId { get; init; }

    [JsonPropertyName("remindFrom")]
    public DateTimeOffset? RemindFrom { get; init; }

    [JsonPropertyName("remindTo")]
    public DateTimeOffset? RemindTo { get; init; }

    [JsonPropertyName("sentFrom")]
    public DateTimeOffset? SentFrom { get; init; }

    [JsonPropertyName("sentTo")]
    public DateTimeOffset? SentTo { get; init; }

    [JsonPropertyName("isSent")]
    public bool? IsSent { get; init; }
}
