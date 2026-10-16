import { useQuery } from "@tanstack/react-query";
import { clinicListSurgeryCalendar } from "../../api/generated/clinic/clinic";
import { mapSurgeryCalendarEntry } from "../../models/surgeries/surgeryMappers";
import { surgeryKeys } from "./surgeryKeys";

export function useSurgeryCalendar(
  range: { from: string; to: string } | undefined,
) {
  const from = range?.from ?? "";
  const to = range?.to ?? "";

  return useQuery({
    queryKey: surgeryKeys.calendar(from, to),
    queryFn: async () => {
      const entries = await clinicListSurgeryCalendar({ from, to });
      return entries.map(mapSurgeryCalendarEntry);
    },
    enabled: Boolean(from && to),
  });
}
