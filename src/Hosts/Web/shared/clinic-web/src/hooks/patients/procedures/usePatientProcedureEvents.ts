import { useQuery } from "@tanstack/react-query";
import { patientProcedureKeys } from "./patientProcedureKeys";
import { listEventItems, listProcedureEvents } from "@vitarish/sea-web";
import { mapToClinicalEventSummary } from "../../../mappers/patients/procedures/mapSeaEventToClinicalStep";
import { mapToEventItemSummary } from "../../../mappers/patients/procedures/mapSeaEventItemToClinicalTimelineItem";

export function usePatientProcedureEvents(procedureId: string) {
  return useQuery({
    queryKey: patientProcedureKeys.events(procedureId),
    queryFn: async () => {
      const dtos = await listProcedureEvents(procedureId);
      return Promise.all(dtos.map(async (dto, index) => {
        const attachments = (await listEventItems(dto.id)).map(mapToEventItemSummary);

        return { ...mapToClinicalEventSummary(dto, index), attachments };
      }));
    },
    enabled: !!procedureId,
  });
}
