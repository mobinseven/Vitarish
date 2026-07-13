namespace Vitarish.Platform.Domain.Entities;

public sealed record LocaleNotificationRecord
{
    public Guid Id { get; init; }
    public string NormalizedLocale { get; init; } = string.Empty;
    public string Locale { get; init; } = string.Empty;
    public string VerificationSmsTemplateId { get; init; } = string.Empty;
    public string SeaReminderEmailSubject { get; init; } = string.Empty;
    public string SeaReminderEmailTextBody { get; init; } = string.Empty;
    public string SeaReminderEmailHtmlBody { get; init; } = string.Empty;
    public string SeaReminderSmsTemplateId { get; init; } = string.Empty;
    public string AppointmentReminderEmailSubject { get; init; } = string.Empty;
    public string AppointmentReminderEmailTextBody { get; init; } = string.Empty;
    public string AppointmentReminderEmailHtmlBody { get; init; } = string.Empty;
    public string AppointmentReminderSmsTemplateId { get; init; } = string.Empty;
    public long Version { get; init; } = 1;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
