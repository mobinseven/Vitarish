import { useQuery } from "@tanstack/react-query";
import { clinicGetSurgery } from "../../api/generated/clinic/clinic";
import { mapSurgeryCalendarEntry } from "../../models/surgeries/surgeryMappers";
import { surgeryKeys } from "./surgeryKeys";

export function useSurgery(surgeryId: string) {
  return useQuery({
    queryKey: surgeryKeys.detail(surgeryId),
    queryFn: async () => mapSurgeryCalendarEntry(await clinicGetSurgery(surgeryId)),
    enabled: Boolean(surgeryId),
  });
}
