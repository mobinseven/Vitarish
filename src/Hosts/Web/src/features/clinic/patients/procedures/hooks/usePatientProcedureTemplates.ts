import { useQuery } from "@tanstack/react-query";
import { patientProcedureKeys } from "./patientProcedureKeys";
import { listTemplateProcedures } from "../api/proceduresApi";

export function usePatientProcedureTemplates() {
  return useQuery({
    queryKey: patientProcedureKeys.templates(),
    queryFn: listTemplateProcedures,
  });
}
