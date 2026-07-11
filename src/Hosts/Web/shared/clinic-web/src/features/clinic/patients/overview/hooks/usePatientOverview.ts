import { useQuery } from "@tanstack/react-query";
import { getPatientDetails } from "../../../../../features/clinic/api/patientApi";
import { patientKeys } from "../../../../../features/clinic/hooks/patientKeys";

export function usePatientOverview(patientId: string) {
  return useQuery({
    queryKey: patientKeys.detail(patientId),
    queryFn: () => getPatientDetails(patientId),
    enabled: !!patientId,
  });
}
