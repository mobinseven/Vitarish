import { useMemo } from "react";
import { CalendarDate, CalendarDateTime, parseDateTime, startOfWeek, endOfWeek, startOfMonth, endOfMonth, getDayOfWeek, parseDate } from "@internationalized/date";
import type { AgendaEventType, AgendaView } from "@heroui-pro/react";
import { getReactAriaLocale, type SupportedLocale } from "@vitarish/web-localization";
import type { ShiftReadDto, ReservationReadDto } from "../../../api/appointments/appointmentsApi";

function calendarDayOfWeek(dayOfWeek: number): number {
  return ((dayOfWeek % 7) + 7) % 7;
}

function parseShiftDateTime(date: CalendarDate, time: string): CalendarDateTime {
  return parseDateTime(`${date.toString()}T${time}:00`);
}

function parseReservationDate(dateStr: string): CalendarDate {
  const datePart = dateStr.includes("T") ? dateStr.split("T")[0] : dateStr;
  return parseDate(datePart);
}

function getVisibleRange(date: CalendarDate, view: AgendaView, locale: SupportedLocale): { start: CalendarDate; end: CalendarDate } {
  switch (view) {
    case "day":
      return { start: date, end: date };
    case "week":
      return {
        start: startOfWeek(date, getReactAriaLocale(locale)),
        end: endOfWeek(date, getReactAriaLocale(locale)),
      };
    case "month":
      return {
        start: startOfWeek(startOfMonth(date), getReactAriaLocale(locale)),
        end: endOfWeek(endOfMonth(date), getReactAriaLocale(locale)),
      };
  }
}

function dateLTE(a: CalendarDate, b: CalendarDate): boolean {
  return a.compare(b) <= 0;
}

function nextDay(date: CalendarDate): CalendarDate {
  return date.add({ days: 1 });
}

function expandShiftsToEvents(
  shifts: ShiftReadDto[],
  range: { start: CalendarDate; end: CalendarDate }
): AgendaEventType[] {
  const events: AgendaEventType[] = [];
  for (const shift of shifts) {
    if (shift.status !== 0) continue;
    const targetDay = calendarDayOfWeek(shift.dayOfWeek);
    let cursor = range.start;
    while (dateLTE(cursor, range.end)) {
      if (getDayOfWeek(cursor, "en-US") === targetDay) {
        events.push({
          id: `shift-${shift.id}-${cursor.toString()}`,
          title: shift.serviceId ?? shift.serviceProviderId ?? "Shift",
          start: parseShiftDateTime(cursor, shift.startTime),
          end: parseShiftDateTime(cursor, shift.endTime),
          isReadOnly: true,
          status: "unconfirmed",
        });
      }
      cursor = nextDay(cursor);
    }
  }
  return events;
}

function reservationsToEvents(
  reservations: ReservationReadDto[],
  shifts: ShiftReadDto[]
): AgendaEventType[] {
  const shiftMap = new Map(shifts.map((s) => [s.id, s]));
  return reservations
    .filter((r) => r.status !== 2 && r.status !== 3)
    .map((r) => {
      const shift = shiftMap.get(r.shiftId);
      const date = parseReservationDate(r.date);
      const startTime = shift?.startTime ?? "09:00";
      const endTime = shift?.endTime ?? "10:00";
      return {
        id: r.id,
        title: shift?.serviceId ?? shift?.serviceProviderId ?? "Reservation",
        start: parseShiftDateTime(date, startTime),
        end: parseShiftDateTime(date, endTime),
        status: r.status === 1 ? "confirmed" : "unconfirmed",
        color: reservationColor(r.status),
      } satisfies AgendaEventType;
    });
}

function reservationColor(status: number): string {
  switch (status) {
    case 1: return "success";
    case 0: return "warning";
    case 6: return "primary";
    case 7: return "default";
    default: return "danger";
  }
}

export function useAgendaEvents(
  date: CalendarDate,
  view: AgendaView,
  shifts: ShiftReadDto[] | undefined,
  reservations: ReservationReadDto[] | undefined,
  locale: SupportedLocale = "fa"
): AgendaEventType[] {
  const range = getVisibleRange(date, view, locale);
  const shiftEvents = useMemo(
    () => (shifts ? expandShiftsToEvents(shifts, range) : []),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [shifts, range.start.toString(), range.end.toString()]
  );
  const reservationEvents = useMemo(
    () => (reservations ? reservationsToEvents(reservations, shifts ?? []) : []),
    [reservations, shifts]
  );
  return useMemo(
    () => [...shiftEvents, ...reservationEvents],
    [shiftEvents, reservationEvents]
  );
}
