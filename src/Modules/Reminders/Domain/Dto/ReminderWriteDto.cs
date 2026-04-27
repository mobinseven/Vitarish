namespace Vitarish.Reminders.Domain.Dto;

public record ReminderWriteDto(
    [property: JsonPropertyName("sourceId")] Guid SourceId,
    [property: JsonPropertyName("kind")] ReminderKind Kind,
    [property: JsonPropertyName("recipientUserId")] string RecipientUserId,
    [property: JsonPropertyName("recipient")] string Recipient,
    [property: JsonPropertyName("subject")] string Subject,
    [property: JsonPropertyName("textBody")] string TextBody,
    [property: JsonPropertyName("remindAt")] DateTimeOffset RemindAt,
    [property: JsonPropertyName("sentAt")] DateTimeOffset? SentAt,
    [property: JsonPropertyName("isSent")] bool IsSent) : EntityWriteDto;
