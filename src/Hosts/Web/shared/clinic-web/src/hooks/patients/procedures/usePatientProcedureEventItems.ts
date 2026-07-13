import { useQuery } from "@tanstack/react-query";
import { patientProcedureKeys } from "./patientProcedureKeys";
import { listEventItems } from "@vitarish/sea-web";
import { mapToEventItemSummary } from "../../../mappers/patients/procedures/mapSeaEventItemToClinicalTimelineItem";

export function usePatientProcedureEventItems(eventId: string) {
  return useQuery({
    queryKey: patientProcedureKeys.eventItems(eventId),
    queryFn: async () => {
      const dtos = await listEventItems(eventId);
      return dtos.map(mapToEventItemSummary);
    },
    enabled: !!eventId,
  });
}
