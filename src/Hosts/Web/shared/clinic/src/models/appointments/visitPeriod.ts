import {
  endOfMonth,
  endOfWeek,
  startOfMonth,
  startOfWeek,
  type CalendarDate,
} from "@internationalized/date";
import { dateValueToIsoDate } from "../../localization";

export const visitPeriods = ["day", "week", "month"] as const;

export type VisitPeriod = (typeof visitPeriods)[number];

export interface VisitPeriodBounds {
  /** First day of the period. */
  start: CalendarDate;
  /** Last day of the period, inclusive. */
  end: CalendarDate;
}

/** The days a period around `focused` covers; weeks start on the locale's first day. */
export function visitPeriodBounds(
  period: VisitPeriod,
  focused: CalendarDate,
  locale: string,
): VisitPeriodBounds {
  if (period === "week") {
    return { start: startOfWeek(focused, locale), end: endOfWeek(focused, locale) };
  }
  if (period === "month") {
    return { start: startOfMonth(focused), end: endOfMonth(focused) };
  }
  return { start: focused, end: focused };
}

/** The dashboard appointments query's inclusive ISO date range for a period. */
export function visitPeriodRange(period: VisitPeriod, focused: CalendarDate, locale: string) {
  const { start, end } = visitPeriodBounds(period, focused, locale);
  return { dateFrom: dateValueToIsoDate(start), dateTo: dateValueToIsoDate(end) };
}

/** The focused day moved by whole periods; negative steps move back. */
export function shiftVisitPeriod(focused: CalendarDate, period: VisitPeriod, steps: number): CalendarDate {
  if (period === "week") return focused.add({ weeks: steps });
  if (period === "month") return focused.add({ months: steps });
  return focused.add({ days: steps });
}
