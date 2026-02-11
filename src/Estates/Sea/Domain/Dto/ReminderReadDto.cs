namespace Vitarish.Sea.Domain.Dto;

public record ReminderReadDto(
    [property: JsonPropertyName("eventId")] Guid EventId,
    [property: JsonPropertyName("kind")] ReminderKind Kind,
    [property: JsonPropertyName("recipientUserId")] string RecipientUserId,
    [property: JsonPropertyName("recipient")] string Recipient,
    [property: JsonPropertyName("subject")] string Subject,
    [property: JsonPropertyName("textBody")] string TextBody,
    [property: JsonPropertyName("remindAt")] DateTimeOffset RemindAt,
    [property: JsonPropertyName("sentAt")] DateTimeOffset? SentAt,
    [property: JsonPropertyName("isSent")] bool IsSent) : EntityDto
{
    [JsonPropertyName("url")]
    public string Url => $"/sea/reminders/{Id}";
}

