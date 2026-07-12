namespace Vitarish.Platform.Contracts.Responses;

public sealed record LocaleNotificationRecordResponse(
    string NormalizedLocale,
    string Locale,
    string VerificationSmsTemplateId,
    string SeaReminderEmailSubject,
    string SeaReminderEmailTextBody,
    string SeaReminderEmailHtmlBody,
    string SeaReminderSmsTemplateId,
    string AppointmentReminderEmailSubject,
    string AppointmentReminderEmailTextBody,
    string AppointmentReminderEmailHtmlBody,
    string AppointmentReminderSmsTemplateId);
