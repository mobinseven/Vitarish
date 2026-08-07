using Vitarish.Entities.Contracts.Models;

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
    [property: JsonPropertyName("isSent")] bool IsSent,
    [property: JsonPropertyName("sourceKind")] string SourceKind = "manual",
    [property: JsonPropertyName("templatePurpose")] string TemplatePurpose = "",
    [property: JsonPropertyName("templateVariablesJson")] string? TemplateVariablesJson = null) : EntityWriteDto;
