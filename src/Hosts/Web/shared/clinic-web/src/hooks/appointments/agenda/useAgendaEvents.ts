import { useMemo } from "react";
import { CalendarDate, startOfWeek, endOfWeek, startOfMonth, endOfMonth, getDayOfWeek, parseDate } from "@internationalized/date";
import type { AgendaEventType, AgendaView } from "@heroui-pro/react";
import { getReactAriaLocale, type SupportedLocale } from "@vitarish/web-localization";
import { parseIsoDateTimeValue } from "../../../localization";
import type { ShiftReadDto, ReservationReadDto, ServiceReadDto, ServiceProviderReadDto } from "../../../api/appointments/appointmentsApi";
import type { ShiftCapacity } from "../useShiftCapacity";

function calendarDayOfWeek(dayOfWeek: number): number {
  return ((dayOfWeek % 7) + 7) % 7;
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

function shiftColor(available: number, total: number): string {
  if (total <= 0) return "var(--default)";
  const ratio = available / total;
  if (ratio <= 0) return "var(--danger)";
  if (ratio <= 0.3) return "var(--warning)";
  return "var(--accent)";
}

function expandShiftsToEvents(
  shifts: ShiftReadDto[],
  range: { start: CalendarDate; end: CalendarDate },
  capacityMap: Map<string, ShiftCapacity> | undefined,
  serviceMap: Map<string, string>,
  providerMap: Map<string, string>,
  shiftLabel = "Shift",
  formatShiftTimeValue = (time: string) => time,
  formatCapacityValue = (available: number, total: number) => `${available}/${total}`,
): AgendaEventType[] {
  const events: AgendaEventType[] = [];
  for (const shift of shifts) {
    // Show all shifts the user defined — do not filter by status
    const targetDay = calendarDayOfWeek(shift.dayOfWeek);
    let cursor = range.start;
    while (dateLTE(cursor, range.end)) {
      if (getDayOfWeek(cursor, "en-US") === targetDay) {
        const dateStr = cursor.toString();
        const cap = capacityMap?.get(`${shift.id}|${dateStr}`);
        const capacityLabel = cap
          ? formatCapacityValue(cap.availableSeats, cap.totalSeats)
          : formatCapacityValue(shift.totalSeats, shift.totalSeats);
        const capColor = cap
          ? shiftColor(cap.availableSeats, cap.totalSeats)
          : "var(--accent)";

        const serviceName = shift.serviceId ? serviceMap.get(shift.serviceId) : null;
        const providerName = shift.serviceProviderId ? providerMap.get(shift.serviceProviderId) : null;
        const displayShiftLabel = serviceName ?? providerName ?? shiftLabel;

        events.push({
          id: `shift|${shift.id}|${dateStr}`,
          title: `${displayShiftLabel} · ${formatShiftTimeValue(shift.startTime)}–${formatShiftTimeValue(shift.endTime)} · ${capacityLabel}`,
          start: parseIsoDateTimeValue(cursor.toString(), shift.startTime),
          end: parseIsoDateTimeValue(cursor.toString(), shift.endTime),
          isAllDay: true,
          status: "unconfirmed",
          color: capColor,
        });
      }
      cursor = nextDay(cursor);
    }
  }
  return events;
}

function reservationsToEvents(
  reservations: ReservationReadDto[],
  shifts: ShiftReadDto[],
  serviceMap: Map<string, string>,
  providerMap: Map<string, string>,
): AgendaEventType[] {
  const shiftMap = new Map(shifts.map((s) => [s.id, s]));
  return reservations
    .filter((r) => r.status !== 2 && r.status !== 3)
    .map((r) => {
      const shift = shiftMap.get(r.shiftId);
      const date = parseReservationDate(r.date);
      const startTime = shift?.startTime ?? "09:00";
      const endTime = shift?.endTime ?? "10:00";
      const serviceName = shift?.serviceId ? serviceMap.get(shift.serviceId) : null;
      const providerName = shift?.serviceProviderId ? providerMap.get(shift.serviceProviderId) : null;
      const label = serviceName ?? providerName ?? "Reservation";
      return {
        id: r.id,
        title: label,
        start: parseIsoDateTimeValue(date.toString(), startTime),
        end: parseIsoDateTimeValue(date.toString(), endTime),
        status: r.status === 1 ? "confirmed" : "unconfirmed",
        color: reservationColor(r.status),
      } satisfies AgendaEventType;
    });
}

function reservationColor(status: number): string {
  switch (status) {
    case 1: return "var(--success)";
    case 0: return "var(--warning)";
    case 6: return "var(--accent)";
    case 7: return "var(--default)";
    default: return "var(--danger)";
  }
}

export function useAgendaEvents(
  date: CalendarDate,
  view: AgendaView,
  shifts: ShiftReadDto[] | undefined,
  reservations: ReservationReadDto[] | undefined,
  locale: SupportedLocale = "fa",
  capacityMap?: Map<string, ShiftCapacity>,
  services?: ServiceReadDto[],
  serviceProviders?: ServiceProviderReadDto[],
  shiftLabel = "Shift",
  formatShiftTimeValue = (time: string) => time,
  formatCapacityValue = (available: number, total: number) => `${available}/${total}`,
): AgendaEventType[] {
  const range = getVisibleRange(date, view, locale);

  const serviceMap = useMemo(
    () => new Map(services?.map((s) => [s.id, s.name]) ?? []),
    [services],
  );
  const providerMap = useMemo(
    () => new Map(serviceProviders?.map((p) => [p.id, p.name]) ?? []),
    [serviceProviders],
  );

  const shiftEvents = useMemo(
    () => (shifts ? expandShiftsToEvents(shifts, range, capacityMap, serviceMap, providerMap, shiftLabel, formatShiftTimeValue, formatCapacityValue) : []),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [shifts, range.start.toString(), range.end.toString(), capacityMap, serviceMap, providerMap, shiftLabel, formatShiftTimeValue, formatCapacityValue]
  );
  const reservationEvents = useMemo(
    () => (reservations ? reservationsToEvents(reservations, shifts ?? [], serviceMap, providerMap) : []),
    [reservations, shifts, serviceMap, providerMap]
  );
  return useMemo(
    () => [...shiftEvents, ...reservationEvents],
    [shiftEvents, reservationEvents]
  );
}
