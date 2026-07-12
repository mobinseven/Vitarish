namespace Vitarish.Platform.Contracts.Requests;

public sealed record LocaleNotificationRecordRequest(
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
