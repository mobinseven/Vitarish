import { useCallback } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { clinicFindOrCreatePatient } from "../../../../../api/generated/clinic/clinic";
import { patientKeys } from "../../../../../features/clinic/hooks/patientKeys";
import type { PatientProfileFormData } from "../models/PatientProfileForm";

export function useCreatePatientProfile() {
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: (data: PatientProfileFormData) =>
      clinicFindOrCreatePatient({
        identificationCode: data.identificationCode || null,
        firstName: data.firstName || null,
        lastName: data.lastName || null,
        phone: data.phone || null,
        email: data.email || null,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: patientKeys.all });
    },
  });

  const create = useCallback(
    (data: PatientProfileFormData) => mutation.mutateAsync(data),
    [mutation]
  );

  return {
    create,
    isPending: mutation.isPending,
    error: mutation.error,
    data: mutation.data,
    reset: mutation.reset,
  };
}
