import { useCallback, useMemo } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { getPatientDetails } from "../../../../../features/clinic/api/patientApi";
import { patientKeys } from "../../../../../features/clinic/hooks/patientKeys";
import type { PatientProfileFormData } from "../models/PatientProfileForm";
import { patientToFormData } from "../models/PatientProfileForm";

export function usePatientProfileEditor(patientId: string) {
  const queryClient = useQueryClient();

  const { data: patient, isLoading, error } = useQuery({
    queryKey: patientKeys.detail(patientId),
    queryFn: () => getPatientDetails(patientId),
    enabled: !!patientId,
  });

  const formData: PatientProfileFormData | null = useMemo(
    () => (patient ? patientToFormData(patient) : null),
    [patient]
  );

  const invalidatePatient = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: patientKeys.detail(patientId) });
    queryClient.invalidateQueries({ queryKey: patientKeys.all });
  }, [queryClient, patientId]);

  return {
    patient,
    formData,
    isLoading,
    error,
    invalidatePatient,
  };
}
