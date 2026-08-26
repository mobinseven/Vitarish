import { type CalendarDate } from "@internationalized/date";
import { dateValueToIsoDate } from "@/localization";

export function calendarMonthRange(focusedDate: CalendarDate) {
  const first = focusedDate.set({ day: 1 });
  const last = first.add({ months: 1 }).subtract({ days: 1 });
  const dateFrom = dateValueToIsoDate(first);
  const dateTo = dateValueToIsoDate(last);
  return { dateFrom, dateTo };
}
