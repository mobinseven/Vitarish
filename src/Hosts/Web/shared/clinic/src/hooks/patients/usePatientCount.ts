import { useQuery } from "@tanstack/react-query";
import { countPatients } from "../../api/patients/patientApi";
import { patientKeys } from "./patientKeys";

export function usePatientCount(search?: string, tags?: string[]) {
  return useQuery({
    queryKey: patientKeys.count(search, tags),
    queryFn: () => countPatients({ search: search ?? null, tags }),
  });
}
