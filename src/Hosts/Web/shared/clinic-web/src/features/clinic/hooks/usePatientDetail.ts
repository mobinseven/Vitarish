import { useQuery } from "@tanstack/react-query";
import { getPatientDetails } from "../api/patientApi";
import { patientKeys } from "./patientKeys";

export function usePatientDetail(patientId: string) {
  return useQuery({
    queryKey: patientKeys.detail(patientId),
    queryFn: () => getPatientDetails(patientId),
    enabled: !!patientId,
  });
}
