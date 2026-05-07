namespace Vitarish.Communications.Domain.Dto;

public record ReminderReadDto : EntityReadDto
{
    [JsonPropertyName("sourceId")]
    public Guid SourceId { get; set; }

    [JsonPropertyName("kind")]
    public ReminderKind Kind { get; set; }

    [JsonPropertyName("recipientUserId")]
    public string RecipientUserId { get; set; } = default!;

    [JsonPropertyName("recipient")]
    public string Recipient { get; set; } = default!;

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = default!;

    [JsonPropertyName("textBody")]
    public string TextBody { get; set; } = default!;

    [JsonPropertyName("remindAt")]
    public DateTimeOffset RemindAt { get; set; }

    [JsonPropertyName("sentAt")]
    public DateTimeOffset? SentAt { get; set; }

    [JsonPropertyName("isSent")]
    public bool IsSent { get; set; }

    [JsonPropertyName("url")]
    public string Url => $"/reminders/{Id}";
}
