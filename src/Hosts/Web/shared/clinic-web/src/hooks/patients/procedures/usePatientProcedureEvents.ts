import { useQuery } from "@tanstack/react-query";
import { patientProcedureKeys } from "./patientProcedureKeys";
import { listProcedureEvents } from "@vitarish/sea-web";
import { mapToClinicalEventSummary } from "../../../mappers/patients/procedures/mapSeaEventToClinicalStep";

export function usePatientProcedureEvents(procedureId: string) {
  return useQuery({
    queryKey: patientProcedureKeys.events(procedureId),
    queryFn: async () => {
      const dtos = await listProcedureEvents(procedureId);
      return dtos.map((dto, index) => mapToClinicalEventSummary(dto, index));
    },
    enabled: !!procedureId,
  });
}
