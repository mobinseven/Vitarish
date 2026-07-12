namespace Vitarish.Platform.Application.Contracts;

public sealed record LocaleNotificationRecordWriteModel(
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
