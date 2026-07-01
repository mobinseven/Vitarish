using System.Globalization;

namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

internal sealed class TaminDateFormatter : ITaminDateFormatter
{
    public string FormatDate(DateTime date) => FormatDate(DateOnly.FromDateTime(date));

    public string FormatDate(DateOnly date)
    {
        if (date == default)
        {
            throw new ArgumentException("Date cannot be the default value.", nameof(date));
        }

        DateTime dateTime = date.ToDateTime(TimeOnly.MinValue);
        PersianCalendar calendar = new();
        if (dateTime < calendar.MinSupportedDateTime || dateTime > calendar.MaxSupportedDateTime)
        {
            throw new ArgumentOutOfRangeException(nameof(date), $"Date must be between {calendar.MinSupportedDateTime:yyyy-MM-dd} and {calendar.MaxSupportedDateTime:yyyy-MM-dd} for Persian calendar conversion.");
        }

        int year = calendar.GetYear(dateTime);
        int month = calendar.GetMonth(dateTime);
        int day = calendar.GetDayOfMonth(dateTime);
        string formatted = FormattableString.Invariant($"{year:0000}{month:00}{day:00}");

        if (!IsValidProviderDate(formatted))
        {
            throw new InvalidOperationException($"Formatted Tamin provider date '{formatted}' is invalid.");
        }

        return formatted;
    }

    public bool IsValidProviderDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 8)
        {
            return false;
        }

        return value.All(char.IsDigit);
    }
}
