import { type CalendarDate } from "@internationalized/date";
import { dateValueToIsoDate } from "@/localization";

export function calendarDayRange(focusedDate: CalendarDate) {
  const date = dateValueToIsoDate(focusedDate);
  return { dateFrom: date, dateTo: date };
}
