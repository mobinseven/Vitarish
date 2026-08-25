import { useQuery } from "@tanstack/react-query";
import { patientProcedureKeys } from "./patientProcedureKeys";
import { listProcedures } from "@vitarish/sea";
import { mapToPatientProcedureSummary } from "../../../mappers/patients/procedures/mapSeaProcedureToPatientProcedure";

export function usePatientProcedures(patientId: string) {
  return useQuery({
    queryKey: patientProcedureKeys.list(patientId),
    queryFn: async () => {
      const dtos = await listProcedures(patientId);
      return dtos.map(mapToPatientProcedureSummary);
    },
    enabled: !!patientId,
  });
}
