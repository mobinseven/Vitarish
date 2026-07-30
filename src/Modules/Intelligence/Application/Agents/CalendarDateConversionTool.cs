using System.Globalization;
using System.Text.Json;

namespace Vitarish.Intelligence.Application.Agents;

public interface IIntelligenceAgentTool : IAgentTool;

public sealed class CalendarDateConversionTool : IIntelligenceAgentTool
{
    private static readonly PersianCalendar PersianCalendar = new();

    public AgentToolDefinition Definition { get; } = new(
        "convert_calendar_date",
        "Convert an exact date between Gregorian and Persian calendars. Always use this tool instead of calculating calendar conversions.",
        """
        {"type":"object","properties":{"date":{"type":"string","description":"Date in yyyy-MM-dd format using the source calendar."},"sourceCalendar":{"type":"string","enum":["gregorian","persian"]}},"required":["date","sourceCalendar"],"additionalProperties":false}
        """);

    public Task<AgentToolResult> ExecuteAsync(
        AgentToolCall call,
        AgentExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using JsonDocument arguments = JsonDocument.Parse(call.ArgumentsJson);
            string date = NormalizeDigits(arguments.RootElement.GetProperty("date").GetString() ?? "");
            string sourceCalendar = arguments.RootElement.GetProperty("sourceCalendar").GetString() ?? "";
            DateOnly gregorian = sourceCalendar switch
            {
                "gregorian" => ParseGregorian(date),
                "persian" => ParsePersian(date),
                _ => throw new FormatException("sourceCalendar must be gregorian or persian.")
            };

            int persianYear = PersianCalendar.GetYear(gregorian.ToDateTime(TimeOnly.MinValue));
            int persianMonth = PersianCalendar.GetMonth(gregorian.ToDateTime(TimeOnly.MinValue));
            int persianDay = PersianCalendar.GetDayOfMonth(gregorian.ToDateTime(TimeOnly.MinValue));
            string gregorianDate = gregorian.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string persianDate = FormattableString.Invariant($"{persianYear:0000}-{persianMonth:00}-{persianDay:00}");

            return Task.FromResult(new AgentToolResult(call.Id, JsonSerializer.Serialize(new
            {
                gregorian = new
                {
                    date = gregorianDate,
                    year = gregorian.Year,
                    month = gregorian.Month,
                    day = gregorian.Day
                },
                persian = new
                {
                    date = persianDate,
                    year = persianYear,
                    month = persianMonth,
                    day = persianDay
                }
            })));
        }
        catch (Exception exception) when (exception is FormatException or ArgumentOutOfRangeException or JsonException)
        {
            return Task.FromResult(new AgentToolResult(
                call.Id,
                JsonSerializer.Serialize(new
                {
                    error = new
                    {
                        code = "invalid_calendar_date",
                        message = "Provide a valid date in yyyy-MM-dd format and identify its source calendar."
                    }
                }),
                IsError: true));
        }
    }

    private static DateOnly ParseGregorian(string value) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date)
            ? date
            : throw new FormatException("The Gregorian date is invalid.");

    private static DateOnly ParsePersian(string value)
    {
        string[] parts = value.Split('-');
        if (parts.Length != 3 ||
            !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int year) ||
            !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int month) ||
            !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out int day))
        {
            throw new FormatException("The Persian date is invalid.");
        }

        return DateOnly.FromDateTime(PersianCalendar.ToDateTime(year, month, day, 0, 0, 0, 0));
    }

    private static string NormalizeDigits(string value)
    {
        ReadOnlySpan<char> persianDigits = "۰۱۲۳۴۵۶۷۸۹";
        ReadOnlySpan<char> arabicDigits = "٠١٢٣٤٥٦٧٨٩";
        char[] normalized = value.ToCharArray();
        for (int index = 0; index < normalized.Length; index++)
        {
            int digit = persianDigits.IndexOf(normalized[index]);
            if (digit < 0) digit = arabicDigits.IndexOf(normalized[index]);
            if (digit >= 0) normalized[index] = (char)('0' + digit);
        }

        return new string(normalized);
    }
}
