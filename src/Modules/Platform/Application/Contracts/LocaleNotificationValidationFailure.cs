namespace Vitarish.Platform.Application.Contracts;

public enum LocaleNotificationValidationRule
{
    LocaleRequired,
    LocaleInvalid,
    VerificationSmsTemplateIdRequired,
    SeaReminderEmailSubjectRequired,
    SeaReminderEmailTextBodyRequired,
    SeaReminderEmailHtmlBodyRequired,
    SeaReminderSmsTemplateIdRequired,
    AppointmentReminderEmailSubjectRequired,
    AppointmentReminderEmailTextBodyRequired,
    AppointmentReminderEmailHtmlBodyRequired,
    AppointmentReminderSmsTemplateIdRequired,
    UnknownPlaceholder,
    MissingPlaceholder,
    ImmutableLocale
}

public sealed record LocaleNotificationValidationFailure(
    LocaleNotificationValidationRule Rule,
    string? Field = null,
    string? Placeholder = null);
