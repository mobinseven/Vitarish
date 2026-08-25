import { useQuery } from "@tanstack/react-query";
import { countPatients } from "../../api/patients/patientApi";
import { patientKeys } from "./patientKeys";

export function usePatientCount(search?: string) {
  return useQuery({
    queryKey: patientKeys.count(search),
    queryFn: () => countPatients({ search: search ?? null }),
  });
}
