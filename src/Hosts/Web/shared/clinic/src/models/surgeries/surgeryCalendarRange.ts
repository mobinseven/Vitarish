import {
  parseAbsolute,
  startOfMonth,
  startOfWeek,
  toCalendarDate,
  toZoned,
  type CalendarDate,
} from "@internationalized/date";

export type SurgeryCalendarView = "month" | "week";

export interface SurgeryCalendarRange {
  /** ISO instant of the first visible day's midnight, inclusive. */
  from: string;
  /** ISO instant of the day after the last visible day's midnight, exclusive. */
  to: string;
  /** The visible days, ordered from the locale's first day of the week. */
  days: CalendarDate[];
}

const daysInWeek = 7;
const weeksInMonth = 6;

/** The visible calendar window, as the API's `[from, to)` instant bounds and its days. */
export function surgeryCalendarRange(
  view: SurgeryCalendarView,
  focused: CalendarDate,
  timeZone: string,
  locale: string,
): SurgeryCalendarRange {
  const start =
    view === "month"
      ? startOfWeek(startOfMonth(focused), locale)
      : startOfWeek(focused, locale);
  const dayCount = view === "month" ? weeksInMonth * daysInWeek : daysInWeek;
  const days = Array.from({ length: dayCount }, (_, index) =>
    start.add({ days: index }),
  );
  const endExclusive = start.add({ days: dayCount });

  return {
    from: toZoned(days[0], timeZone).toAbsoluteString(),
    to: toZoned(endExclusive, timeZone).toAbsoluteString(),
    days,
  };
}

/** The clinic-local yyyy-mm-dd key of an instant, for grouping surgeries by day. */
export function surgeryDayKey(instant: string, timeZone: string): string {
  return toCalendarDate(parseAbsolute(instant, timeZone)).toString();
}
