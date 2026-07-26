import { useMemo } from "react";
import { parseDate, getDayOfWeek } from "@internationalized/date";
import type { ShiftReadDto, ReservationReadDto } from "../../api/appointments/appointmentsApi";

export interface ShiftCapacity {
  totalSeats: number;
  bookedCount: number;
  availableSeats: number;
}

/**
 * Computes available seats for each shift on each visible date.
 * Returns a Map keyed by `${shiftId}|${dateString}`.
 *
 * A reservation is "booked" for a shift when:
 * - It is active (status 0 or 1 — pending or confirmed)
 * - Its date falls on the shift's dayOfWeek
 * - Its time overlaps the shift's time window
 */
export function useShiftCapacity(
  shifts: ShiftReadDto[] | undefined,
  reservations: ReservationReadDto[] | undefined,
  visibleDates: string[],
): Map<string, ShiftCapacity> {
  return useMemo(() => {
    const map = new Map<string, ShiftCapacity>();
    if (!shifts || !reservations) return map;

    const activeReservations = reservations.filter(
      (r) => r.status === 0 || r.status === 1,
    );

    for (const shift of shifts) {
      // Show capacity for all shifts — do not filter by status
      for (const dateStr of visibleDates) {
        const date = parseDate(dateStr);
        const dayOfWeek = getDayOfWeek(date, "en-US");
        if (dayOfWeek !== shift.dayOfWeek) continue;

        const key = `${shift.id}|${dateStr}`;
        const bookedCount = activeReservations.filter((r) => {
          if (r.shiftId !== shift.id) return false;
          const rDate = r.date.includes("T") ? r.date.split("T")[0] : r.date;
          return rDate === dateStr;
        }).length;

        map.set(key, {
          totalSeats: shift.totalSeats,
          bookedCount,
          availableSeats: Math.max(0, shift.totalSeats - bookedCount),
        });
      }
    }

    return map;
  }, [shifts, reservations, visibleDates]);
}
