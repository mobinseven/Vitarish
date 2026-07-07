import { useQuery } from "@tanstack/react-query";
import { patientProcedureKeys } from "./patientProcedureKeys";
import { getEvent, listProcedureEvents } from "../api/proceduresApi";
import { mapToClinicalEventDetail } from "../mappers/mapSeaEventToClinicalStep";

export function usePatientProcedureEventDetail(eventId: string) {
  return useQuery({
    queryKey: patientProcedureKeys.eventDetail(eventId),
    queryFn: async () => {
      const dto = await getEvent(eventId);
      const events = await listProcedureEvents(dto.procedureId);
      const position = events.findIndex((e) => e.id === eventId);
      return mapToClinicalEventDetail(dto, position >= 0 ? position : 0);
    },
    enabled: !!eventId,
  });
}
