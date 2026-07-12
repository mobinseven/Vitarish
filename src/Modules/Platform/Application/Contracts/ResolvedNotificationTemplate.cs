namespace Vitarish.Platform.Application.Contracts;

public sealed record ResolvedNotificationTemplate(
    NotificationPurpose Purpose,
    string? EmailSubject,
    string? EmailTextBody,
    string? EmailHtmlBody,
    string? SmsTemplateId,
    IReadOnlyDictionary<string, string>? SmsParameters);
