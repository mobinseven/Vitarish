import { useQuery } from "@tanstack/react-query";
import { patientProcedureKeys } from "./patientProcedureKeys";
import { getProcedure } from "@vitarish/sea-web";
import { mapToPatientProcedureDetail } from "../mappers/mapSeaProcedureToPatientProcedure";

export function usePatientProcedureDetail(procedureId: string) {
  return useQuery({
    queryKey: patientProcedureKeys.detail(procedureId),
    queryFn: async () => {
      const dto = await getProcedure(procedureId);
      return mapToPatientProcedureDetail(dto);
    },
    enabled: !!procedureId,
  });
}
