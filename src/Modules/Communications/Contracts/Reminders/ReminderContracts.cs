namespace Vitarish.Communications.Contracts.Reminders;
public enum ReminderKind { Email, Sms }
public sealed record ReminderRecipientReadDto { public ReminderKind Kind { get; init; } public string? RecipientUserId { get; init; } public string? Recipient { get; init; } }
public sealed record ReminderDescriptorDto(Guid? SourceId = null, bool? IsSent = null, string? SourceKind = null);
public sealed record ReminderWriteDto(Guid SourceId, ReminderKind Kind, string? RecipientUserId, string? Recipient, string? Subject, string? TextBody, DateTime RemindAt, DateTime? SentAt, bool IsSent, string SourceKind = "manual", string TemplatePurpose = "", string? TemplateVariablesJson = null);
public sealed record ReminderReadDto(Guid Id, Guid SourceId, ReminderKind Kind, string? RecipientUserId, string? Recipient, string? Subject, string? TextBody, DateTime RemindAt, DateTime? SentAt, bool IsSent, string? CreatedBy, DateTime CreatedAt, string SourceKind = "manual", string TemplatePurpose = "", string? TemplateVariablesJson = null, string? ProviderMessageId = null, DateTime? ProviderSubmittedAt = null);
public sealed record ReminderRangeCreateRequest(Guid SourceId, IReadOnlyList<ReminderRecipientReadDto> Recipients, string Subject, string TextBody, DateTime RemindAt, string SourceKind = "manual", string TemplatePurpose = "", string? TemplateVariablesJson = null);
public sealed record ReminderUpdateRequest(Guid Id, ReminderKind Kind, string RecipientUserId, string Recipient, string Subject, string TextBody, DateTime RemindAt, string? SourceKind = null, string? TemplatePurpose = null, string? TemplateVariablesJson = null);
public sealed record MarkReminderSentRequest(DateTime SentAt);
