namespace Vitarish.Communications.Domain.Dto;

public record ReminderWriteDto(
    [property: JsonPropertyName("sourceId")] Guid SourceId,
    [property: JsonPropertyName("kind")] ReminderKind Kind,
    [property: JsonPropertyName("recipientUserId")] string? RecipientUserId,
    [property: JsonPropertyName("recipient")] string Recipient,
    [property: JsonPropertyName("subject")] string Subject,
    [property: JsonPropertyName("textBody")] string TextBody,
    [property: JsonPropertyName("remindAt")] DateTime RemindAt,
    [property: JsonPropertyName("sentAt")] DateTime? SentAt,
    [property: JsonPropertyName("isSent")] bool IsSent) : EntityWriteDto;
