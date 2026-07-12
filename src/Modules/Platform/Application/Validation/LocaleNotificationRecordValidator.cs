using System.Globalization;
using System.Text.RegularExpressions;
using Vitarish.Platform.Application.Contracts;

namespace Vitarish.Platform.Application.Validation;

public static partial class LocaleNotificationRecordValidator
{
    public const string LocaleRequired = "Locale is required.";
    public const string LocaleInvalid = "Locale is not a valid canonical culture name.";
    public const string VerificationSmsTemplateIdRequired = "Verification SMS template ID is required.";
    public const string SeaReminderEmailSubjectRequired = "Sea reminder email subject is required.";
    public const string SeaReminderEmailTextBodyRequired = "Sea reminder email text body is required.";
    public const string SeaReminderEmailHtmlBodyRequired = "Sea reminder email HTML body is required.";
    public const string SeaReminderSmsTemplateIdRequired = "Sea reminder SMS template ID is required.";
    public const string AppointmentReminderEmailSubjectRequired = "Appointment reminder email subject is required.";
    public const string AppointmentReminderEmailTextBodyRequired = "Appointment reminder email text body is required.";
    public const string AppointmentReminderEmailHtmlBodyRequired = "Appointment reminder email HTML body is required.";
    public const string AppointmentReminderSmsTemplateIdRequired = "Appointment reminder SMS template ID is required.";
    public const string UnknownPlaceholder = "Template contains unknown placeholder '{0}'.";
    public const string MissingPlaceholder = "Template is missing required placeholder '{0}'.";

    private static readonly HashSet<string> SeaReminderPlaceholders = new(StringComparer.Ordinal)
    {
        "{EventTitle}", "{ProcedureName}", "{ProjectName}"
    };

    private static readonly HashSet<string> AppointmentReminderPlaceholders = new(StringComparer.Ordinal)
    {
        "{OccurrenceStart}"
    };

    public static string? Validate(LocaleNotificationRecordWriteModel? model)
    {
        if (model is null || string.IsNullOrWhiteSpace(model.Locale))
        {
            return LocaleRequired;
        }

        if (!TryNormalizeLocale(model.Locale, out _))
        {
            return LocaleInvalid;
        }

        if (string.IsNullOrWhiteSpace(model.VerificationSmsTemplateId))
        {
            return VerificationSmsTemplateIdRequired;
        }

        string? error = ValidateSeaReminder(model);
        if (error is not null) return error;

        error = ValidateAppointmentReminder(model);
        return error;
    }

    public static string? ValidateNormalizedLocale(string? locale, out string? normalizedLocale)
    {
        normalizedLocale = null;
        if (string.IsNullOrWhiteSpace(locale))
        {
            return LocaleRequired;
        }

        if (!TryNormalizeLocale(locale, out string? normalized))
        {
            return LocaleInvalid;
        }

        normalizedLocale = normalized;
        return null;
    }

    private static string? ValidateSeaReminder(LocaleNotificationRecordWriteModel model)
    {
        if (string.IsNullOrWhiteSpace(model.SeaReminderEmailSubject))
            return SeaReminderEmailSubjectRequired;
        if (string.IsNullOrWhiteSpace(model.SeaReminderEmailTextBody))
            return SeaReminderEmailTextBodyRequired;
        if (string.IsNullOrWhiteSpace(model.SeaReminderEmailHtmlBody))
            return SeaReminderEmailHtmlBodyRequired;
        if (string.IsNullOrWhiteSpace(model.SeaReminderSmsTemplateId))
            return SeaReminderSmsTemplateIdRequired;

        string? error = ValidatePlaceholders(model.SeaReminderEmailTextBody, SeaReminderPlaceholders, "Sea reminder email text");
        if (error is not null) return error;

        error = ValidatePlaceholders(model.SeaReminderEmailHtmlBody, SeaReminderPlaceholders, "Sea reminder email HTML");
        return error;
    }

    private static string? ValidateAppointmentReminder(LocaleNotificationRecordWriteModel model)
    {
        if (string.IsNullOrWhiteSpace(model.AppointmentReminderEmailSubject))
            return AppointmentReminderEmailSubjectRequired;
        if (string.IsNullOrWhiteSpace(model.AppointmentReminderEmailTextBody))
            return AppointmentReminderEmailTextBodyRequired;
        if (string.IsNullOrWhiteSpace(model.AppointmentReminderEmailHtmlBody))
            return AppointmentReminderEmailHtmlBodyRequired;
        if (string.IsNullOrWhiteSpace(model.AppointmentReminderSmsTemplateId))
            return AppointmentReminderSmsTemplateIdRequired;

        string? error = ValidatePlaceholders(model.AppointmentReminderEmailTextBody, AppointmentReminderPlaceholders, "Appointment reminder email text");
        if (error is not null) return error;

        error = ValidatePlaceholders(model.AppointmentReminderEmailHtmlBody, AppointmentReminderPlaceholders, "Appointment reminder email HTML");
        return error;
    }

    private static string? ValidatePlaceholders(string template, HashSet<string> allowedPlaceholders, string fieldName)
    {
        HashSet<string> foundPlaceholders = ExtractPlaceholders(template);

        foreach (string placeholder in foundPlaceholders)
        {
            if (!allowedPlaceholders.Contains(placeholder))
            {
                return string.Format(UnknownPlaceholder, placeholder);
            }
        }

        foreach (string required in allowedPlaceholders)
        {
            if (!foundPlaceholders.Contains(required))
            {
                return string.Format(MissingPlaceholder, required);
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
