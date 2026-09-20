import { createCalendar, parseDate, parseDateTime, toCalendar, type CalendarDateTime, type DateValue } from "@internationalized/date";
import { getDateCalendar } from "./dateLocale";
import type { SupportedLocale } from "./locales";

const isoDatePattern = /^\d{4}-\d{2}-\d{2}$/;

export function parseIsoDateValue(value: string | null | undefined): DateValue | null {
  if (!value) return null;
  if (!isoDatePattern.test(value)) return null;
  try {
    return parseDate(value);
  } catch {
    return null;
  }
}

export function dateValueToIsoDate(value: DateValue | null): string {
  return value ? toCalendar(value, createCalendar("gregory")).toString() : "";
}

export function toLocalizedDateValue(value: DateValue | null, locale: SupportedLocale): DateValue | null {
  return value ? toCalendar(value, createCalendar(getDateCalendar(locale))) : null;
}

/** Parses an API date plus either HH:mm or HH:mm:ss time into one date-time value. */
export function parseIsoDateTimeValue(
  date: string,
  time: string,
): CalendarDateTime {
  const normalizedTime = time.length === 5 ? `${time}:00` : time;
  return parseDateTime(`${date}T${normalizedTime}`);
}
