import { useQuery } from "@tanstack/react-query";
import { patientProcedureKeys } from "./patientProcedureKeys";
import { listTemplateProcedures } from "@vitarish/sea-web";

export function usePatientProcedureTemplates() {
  return useQuery({
    queryKey: patientProcedureKeys.templates(),
    queryFn: listTemplateProcedures,
  });
}
