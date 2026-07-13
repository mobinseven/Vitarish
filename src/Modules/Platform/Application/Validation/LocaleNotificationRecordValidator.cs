using System.Globalization;
using System.Text.RegularExpressions;
using Vitarish.Platform.Application.Contracts;

namespace Vitarish.Platform.Application.Validation;

public static partial class LocaleNotificationRecordValidator
{
    public const string LocaleField = "locale";
    public const string VerificationSmsTemplateIdField = "verificationSmsTemplateId";
    public const string SeaReminderEmailSubjectField = "seaReminderEmailSubject";
    public const string SeaReminderEmailTextBodyField = "seaReminderEmailTextBody";
    public const string SeaReminderEmailHtmlBodyField = "seaReminderEmailHtmlBody";
    public const string SeaReminderSmsTemplateIdField = "seaReminderSmsTemplateId";
    public const string AppointmentReminderEmailSubjectField = "appointmentReminderEmailSubject";
    public const string AppointmentReminderEmailTextBodyField = "appointmentReminderEmailTextBody";
    public const string AppointmentReminderEmailHtmlBodyField = "appointmentReminderEmailHtmlBody";
    public const string AppointmentReminderSmsTemplateIdField = "appointmentReminderSmsTemplateId";

    private static readonly HashSet<string> SeaReminderPlaceholders = new(StringComparer.Ordinal)
    {
        "{EventTitle}", "{ProcedureName}", "{ProjectName}"
    };

    private static readonly HashSet<string> AppointmentReminderPlaceholders = new(StringComparer.Ordinal)
    {
        "{OccurrenceStart}"
    };

    public static LocaleNotificationValidationFailure? Validate(LocaleNotificationRecordWriteModel? model)
    {
        if (model is null || string.IsNullOrWhiteSpace(model.Locale))
        {
            return new(LocaleNotificationValidationRule.LocaleRequired, LocaleField);
        }

        if (!TryNormalizeLocale(model.Locale, out _))
        {
            return new(LocaleNotificationValidationRule.LocaleInvalid, LocaleField);
        }

        if (string.IsNullOrWhiteSpace(model.VerificationSmsTemplateId))
        {
            return new(LocaleNotificationValidationRule.VerificationSmsTemplateIdRequired, VerificationSmsTemplateIdField);
        }

        LocaleNotificationValidationFailure? error = ValidateSeaReminder(model);
        if (error is not null) return error;

        error = ValidateAppointmentReminder(model);
        return error;
    }

    public static LocaleNotificationValidationFailure? ValidateNormalizedLocale(string? locale, out string? normalizedLocale)
    {
        normalizedLocale = null;
        if (string.IsNullOrWhiteSpace(locale))
        {
            return new(LocaleNotificationValidationRule.LocaleRequired, LocaleField);
        }

        if (!TryNormalizeLocale(locale, out string? normalized))
        {
            return new(LocaleNotificationValidationRule.LocaleInvalid, LocaleField);
        }

        normalizedLocale = normalized;
        return null;
    }

    public static IReadOnlyList<LocaleNotificationRequirement> GetRequirements() =>
    [
        new LocaleNotificationRequirement(
            LocaleNotificationPurpose.SeaReminder,
            [SeaReminderEmailTextBodyField, SeaReminderEmailHtmlBodyField],
            [.. SeaReminderPlaceholders],
            [.. SeaReminderPlaceholders]),
        new LocaleNotificationRequirement(
            LocaleNotificationPurpose.AppointmentReminder,
            [AppointmentReminderEmailTextBodyField, AppointmentReminderEmailHtmlBodyField],
            [.. AppointmentReminderPlaceholders],
            [.. AppointmentReminderPlaceholders])
    ];

    private static LocaleNotificationValidationFailure? ValidateSeaReminder(LocaleNotificationRecordWriteModel model)
    {
        if (string.IsNullOrWhiteSpace(model.SeaReminderEmailSubject))
            return new(LocaleNotificationValidationRule.SeaReminderEmailSubjectRequired, SeaReminderEmailSubjectField);
        if (string.IsNullOrWhiteSpace(model.SeaReminderEmailTextBody))
            return new(LocaleNotificationValidationRule.SeaReminderEmailTextBodyRequired, SeaReminderEmailTextBodyField);
        if (string.IsNullOrWhiteSpace(model.SeaReminderEmailHtmlBody))
            return new(LocaleNotificationValidationRule.SeaReminderEmailHtmlBodyRequired, SeaReminderEmailHtmlBodyField);
        if (string.IsNullOrWhiteSpace(model.SeaReminderSmsTemplateId))
            return new(LocaleNotificationValidationRule.SeaReminderSmsTemplateIdRequired, SeaReminderSmsTemplateIdField);

        LocaleNotificationValidationFailure? error = ValidatePlaceholders(model.SeaReminderEmailTextBody, SeaReminderPlaceholders, SeaReminderEmailTextBodyField);
        if (error is not null) return error;

        error = ValidatePlaceholders(model.SeaReminderEmailHtmlBody, SeaReminderPlaceholders, SeaReminderEmailHtmlBodyField);
        return error;
    }

    private static LocaleNotificationValidationFailure? ValidateAppointmentReminder(LocaleNotificationRecordWriteModel model)
    {
        if (string.IsNullOrWhiteSpace(model.AppointmentReminderEmailSubject))
            return new(LocaleNotificationValidationRule.AppointmentReminderEmailSubjectRequired, AppointmentReminderEmailSubjectField);
        if (string.IsNullOrWhiteSpace(model.AppointmentReminderEmailTextBody))
            return new(LocaleNotificationValidationRule.AppointmentReminderEmailTextBodyRequired, AppointmentReminderEmailTextBodyField);
        if (string.IsNullOrWhiteSpace(model.AppointmentReminderEmailHtmlBody))
            return new(LocaleNotificationValidationRule.AppointmentReminderEmailHtmlBodyRequired, AppointmentReminderEmailHtmlBodyField);
        if (string.IsNullOrWhiteSpace(model.AppointmentReminderSmsTemplateId))
            return new(LocaleNotificationValidationRule.AppointmentReminderSmsTemplateIdRequired, AppointmentReminderSmsTemplateIdField);

        LocaleNotificationValidationFailure? error = ValidatePlaceholders(model.AppointmentReminderEmailTextBody, AppointmentReminderPlaceholders, AppointmentReminderEmailTextBodyField);
        if (error is not null) return error;

        error = ValidatePlaceholders(model.AppointmentReminderEmailHtmlBody, AppointmentReminderPlaceholders, AppointmentReminderEmailHtmlBodyField);
        return error;
    }

    private static LocaleNotificationValidationFailure? ValidatePlaceholders(string template, HashSet<string> allowedPlaceholders, string fieldName)
    {
        HashSet<string> foundPlaceholders = ExtractPlaceholders(template);

        foreach (string placeholder in foundPlaceholders)
        {
            if (!allowedPlaceholders.Contains(placeholder))
            {
                return new(LocaleNotificationValidationRule.UnknownPlaceholder, fieldName, placeholder);
            }
        }

        foreach (string required in allowedPlaceholders)
        {
            if (!foundPlaceholders.Contains(required))
            {
                return new(LocaleNotificationValidationRule.MissingPlaceholder, fieldName, required);
            }
        }

        return null;
    }

    private static HashSet<string> ExtractPlaceholders(string template)
    {
        HashSet<string> placeholders = new(StringComparer.Ordinal);
        foreach (Match match in PlaceholderRegex().Matches(template))
        {
            placeholders.Add(match.Value);
        }
        return placeholders;
    }

    public static bool TryNormalizeLocale(string locale, out string? normalized)
    {
        normalized = null;
        try
        {
            CultureInfo culture = new(locale);
            if (!string.Equals(culture.Name, locale, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            normalized = culture.Name;
            return true;
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }

    [GeneratedRegex(@"\{[A-Za-z]+\}")]
    private static partial Regex PlaceholderRegex();
}
