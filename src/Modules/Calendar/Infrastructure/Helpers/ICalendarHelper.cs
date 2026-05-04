namespace Vitarish.Calendar.Infrastructure.Helpers;

/// <summary>
/// Generates and parses RFC 5545 iCalendar strings for VEVENT components.
/// Handles recurrence rules, exception dates, recurrence-id overrides, VTIMEZONE blocks,
/// and occurrence-edit scope splitting.
/// </summary>
public interface ICalendarHelper
{
    /// <summary>
    /// Builds a minimal VEVENT iCalendar string for a non-recurring event.
    /// </summary>
    string BuildVEvent(Guid uid, DateTimeOffset start, DateTimeOffset end, string summary, string timeZone);

    /// <summary>
    /// Builds a recurring VEVENT iCalendar string including the recurrence rule.
    /// </summary>
    string BuildRecurringVEvent(Guid uid, DateTimeOffset start, DateTimeOffset end, string summary, RecurrenceInfo recurrence, string timeZone);

    /// <summary>
    /// Patches an existing iCalendar string to apply the cancellation status.
    /// </summary>
    string ApplyCancellation(string iCalData);

    /// <summary>
    /// Extracts the recurrence information from an existing iCalendar string.
    /// </summary>
    RecurrenceInfo ParseRecurrenceInfo(string iCalData);

    /// <summary>
    /// Expands a recurring VEVENT to its individual occurrences within the given range.
    /// </summary>
    IReadOnlyList<(DateTimeOffset Start, DateTimeOffset End, string RecurrenceId)> ExpandOccurrences(string iCalData, DateTimeOffset rangeStart, DateTimeOffset rangeEnd);

    /// <summary>
    /// Applies a patch to a single occurrence (ThisOccurrence scope): creates an exception iCalendar.
    /// </summary>
    string ApplyThisOccurrenceEdit(string masterICalData, string recurrenceId, string patchedOccurrenceICalData);

    /// <summary>
    /// Applies a patch to all future occurrences (ThisAndFuture scope): splits the master series and
    /// returns the modified master iCalendar.
    /// </summary>
    string ApplyThisAndFutureEdit(string masterICalData, string recurrenceId, string patchedOccurrenceICalData);
}
