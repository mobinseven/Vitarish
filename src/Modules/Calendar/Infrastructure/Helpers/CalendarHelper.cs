using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using ICalCalendar = Ical.Net.Calendar;

namespace Vitarish.Calendar.Infrastructure.Helpers;

/// <summary>
/// RFC 5545 iCalendar helper using the Ical.Net library.
/// </summary>
[RegisterTransient]
internal class CalendarHelper : ICalendarHelper
{
    public string BuildVEvent(Guid uid, DateTimeOffset start, DateTimeOffset end, string summary, string timeZone)
    {
        ICalCalendar cal = new();
        CalendarEvent evt = new()
        {
            Uid = uid.ToString(),
            Summary = summary,
            DtStart = new CalDateTime(start.UtcDateTime, "UTC"),
            DtEnd = new CalDateTime(end.UtcDateTime, "UTC")
        };
        cal.Events.Add(evt);
        return Serialize(cal);
    }

    public string BuildRecurringVEvent(Guid uid, DateTimeOffset start, DateTimeOffset end, string summary, RecurrenceInfo recurrence, string timeZone)
    {
        ICalCalendar cal = new();
        CalendarEvent evt = new()
        {
            Uid = uid.ToString(),
            Summary = summary,
            DtStart = new CalDateTime(start.UtcDateTime, "UTC"),
            DtEnd = new CalDateTime(end.UtcDateTime, "UTC")
        };

        if (!string.IsNullOrWhiteSpace(recurrence.RRule))
        {
            evt.RecurrenceRules.Add(new RecurrencePattern(recurrence.RRule));
        }

        foreach (DateTimeOffset exDate in recurrence.ExDates)
        {
            evt.ExceptionDates.Add(new PeriodList { new Period(new CalDateTime(exDate.UtcDateTime, "UTC")) });
        }

        foreach (DateTimeOffset rDate in recurrence.RDates)
        {
            evt.RecurrenceDates.Add(new PeriodList { new Period(new CalDateTime(rDate.UtcDateTime, "UTC")) });
        }

        cal.Events.Add(evt);
        return Serialize(cal);
    }

    public string ApplyCancellation(string iCalData)
    {
        ICalCalendar cal = ICalCalendar.Load(iCalData);
        foreach (CalendarEvent evt in cal.Events)
        {
            evt.Status = EventStatus.Cancelled;
        }
        return Serialize(cal);
    }

    public RecurrenceInfo ParseRecurrenceInfo(string iCalData)
    {
        ICalCalendar cal = ICalCalendar.Load(iCalData);
        CalendarEvent? evt = cal.Events.FirstOrDefault();

        if (evt is null)
        {
            return new RecurrenceInfo();
        }

        string rRule = evt.RecurrenceRules.FirstOrDefault()?.ToString() ?? string.Empty;

        List<DateTimeOffset> exDates = [
            .. evt.ExceptionDates
               .SelectMany(pl => pl)
               .Select(p => new DateTimeOffset(p.StartTime.Value, TimeSpan.Zero))
        ];

        List<DateTimeOffset> rDates = [
            .. evt.RecurrenceDates
               .SelectMany(pl => pl)
               .Select(p => new DateTimeOffset(p.StartTime.Value, TimeSpan.Zero))
        ];

        return new RecurrenceInfo
        {
            RRule = rRule,
            ExDates = exDates,
            RDates = rDates,
            IsRecurring = evt.RecurrenceRules.Count > 0 || rDates.Count > 0
        };
    }

    public IReadOnlyList<(DateTimeOffset Start, DateTimeOffset End, string RecurrenceId)> ExpandOccurrences(string iCalData, DateTimeOffset rangeStart, DateTimeOffset rangeEnd)
    {
        ICalCalendar cal = ICalCalendar.Load(iCalData);
        CalendarEvent? evt = cal.Events.FirstOrDefault();

        if (evt is null)
        {
            return [];
        }

        DateTime from = rangeStart.UtcDateTime;
        DateTime to = rangeEnd.UtcDateTime;

        IEnumerable<Occurrence> occurrences = cal.GetOccurrences(from, to);

        return [.. occurrences
            .Select(o =>
            {
                DateTimeOffset start = new(o.Period.StartTime.Value, TimeSpan.Zero);
                DateTimeOffset end = new(o.Period.EndTime.Value, TimeSpan.Zero);
                string recId = o.Period.StartTime.Value.ToString("yyyyMMddTHHmmssZ");
                return (start, end, recId);
            })
        ];
    }

    public string ApplyThisOccurrenceEdit(string masterICalData, string recurrenceId, string patchedOccurrenceICalData)
    {
        ICalCalendar master = ICalCalendar.Load(masterICalData);
        CalendarEvent? masterEvt = master.Events.FirstOrDefault();

        if (masterEvt is null)
        {
            return masterICalData;
        }

        // Add an EXDATE for the occurrence being overridden
        if (TryParseRecurrenceId(recurrenceId, out DateTime exDateTime))
        {
            masterEvt.ExceptionDates.Add(new PeriodList { new Period(new CalDateTime(exDateTime, "UTC")) });
        }

        return Serialize(master);
    }

    public string ApplyThisAndFutureEdit(string masterICalData, string recurrenceId, string patchedOccurrenceICalData)
    {
        ICalCalendar master = ICalCalendar.Load(masterICalData);
        CalendarEvent? masterEvt = master.Events.FirstOrDefault();

        if (masterEvt is null)
        {
            return masterICalData;
        }

        // Truncate the master series by setting UNTIL on the RRULE
        if (masterEvt.RecurrenceRules.Count > 0
            && TryParseRecurrenceId(recurrenceId, out DateTime splitPoint))
        {
            RecurrencePattern rrule = masterEvt.RecurrenceRules[0];
            rrule.Until = splitPoint.AddSeconds(-1);
        }

        return Serialize(master);
    }

    private static bool TryParseRecurrenceId(string recurrenceId, out DateTime result) =>
        DateTime.TryParseExact(
            recurrenceId.TrimEnd('Z'),
            "yyyyMMddTHHmmss",
            null,
            System.Globalization.DateTimeStyles.AssumeUniversal,
            out result);

    private static string Serialize(ICalCalendar cal)
    {
        CalendarSerializer serializer = new();
        return serializer.SerializeToString(cal);
    }
}
