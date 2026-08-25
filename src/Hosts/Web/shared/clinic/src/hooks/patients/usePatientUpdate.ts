import { useCallback } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { clinicUpdatePatient } from "../../api/generated/clinic/clinic";
import type { PatientProfileFormData } from "../../models/patients/profile/PatientProfileForm";
import { toPatientFindOrCreateRequest } from "./profile/useCreatePatientProfile";
import { patientKeys } from "./patientKeys";

export function usePatientUpdate() {
  const queryClient = useQueryClient();
  const mutation = useMutation({
    mutationFn: ({ patientId, data }: { patientId: string; data: PatientProfileFormData }) =>
      clinicUpdatePatient(patientId, toPatientFindOrCreateRequest(data)),
    onSuccess: (_patient, { patientId }) => {
      queryClient.invalidateQueries({ queryKey: patientKeys.detail(patientId) });
      queryClient.invalidateQueries({ queryKey: patientKeys.all });
    },
  });

  const update = useCallback(
    (patientId: string, data: PatientProfileFormData) => mutation.mutateAsync({ patientId, data }),
    [mutation]
  );

  return { update, isPending: mutation.isPending, error: mutation.error };
}
