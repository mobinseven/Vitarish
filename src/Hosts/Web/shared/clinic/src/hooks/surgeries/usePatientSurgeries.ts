import { useQuery } from "@tanstack/react-query";
import { clinicListPatientSurgeries } from "../../api/generated/clinic/clinic";
import { mapSurgeryResponse } from "../../models/surgeries/surgeryMappers";
import { surgeryKeys } from "./surgeryKeys";

export function usePatientSurgeries(patientId: string) {
  return useQuery({
    queryKey: surgeryKeys.patient(patientId),
    queryFn: async () => {
      const surgeries = (await clinicListPatientSurgeries(patientId)).map(
        mapSurgeryResponse,
      );
      return surgeries.sort(
        (a, b) =>
          new Date(b.scheduledAt).getTime() - new Date(a.scheduledAt).getTime(),
      );
    },
    enabled: Boolean(patientId),
  });
}
