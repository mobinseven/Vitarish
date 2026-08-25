import { useMemo } from "react";
import { useQueries } from "@tanstack/react-query";
import { appointmentsListBookableDates } from "../../../api/generated/appointments/appointments";
import type { ShiftReadDto } from "../../../api/appointments/appointmentsApi";
import type { ShiftCapacity } from "../useShiftCapacity";

interface BookableShiftOccurrences {
  capacityMap: Map<string, ShiftCapacity>;
  occurrenceKeys: Set<string>;
}

export function useBookableShiftOccurrences(
  shifts: ShiftReadDto[],
  visibleDates: string[],
): BookableShiftOccurrences {
  const from = visibleDates[0];
  const to = visibleDates.at(-1);
  const queries = useQueries({
    queries: shifts.map((shift) => ({
      queryKey: ["appointments", "bookable-dates", shift.id, from, to],
      queryFn: () => appointmentsListBookableDates({ shiftId: shift.id, from: from ?? "", to: to ?? "" }),
      enabled: Boolean(from && to),
      staleTime: 30_000,
    })),
  });

  return useMemo(() => {
    const capacityMap = new Map<string, ShiftCapacity>();
    const occurrenceKeys = new Set<string>();

    for (const query of queries) {
      for (const occurrence of query.data ?? []) {
        if (!occurrence.shiftId || !occurrence.date) continue;

        const totalSeats = Number(occurrence.totalSeats ?? 0);
        const bookedCount = Number(occurrence.reservationCount ?? 0);
        const availableSeats = Number(occurrence.availableSeats ?? 0);
        if (availableSeats <= 0) continue;

        const key = `${occurrence.shiftId}|${occurrence.date}`;
        occurrenceKeys.add(key);
        capacityMap.set(key, { totalSeats, bookedCount, availableSeats });
      }
    }

    return { capacityMap, occurrenceKeys };
  }, [queries]);
}
