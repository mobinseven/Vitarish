import { useCallback } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { clinicFindOrCreatePatient } from "../../../api/generated/clinic/clinic";
import type {
  Education,
  Gender,
  PatientFindOrCreateRequest,
} from "../../../api/generated/clinic/model";
import { patientKeys } from "../patientKeys";
import type { PatientProfileFormData } from "../../../models/patients/profile/PatientProfileForm";

function optionalDateTime(value: string): string | null {
  return value ? `${value}T00:00:00.000Z` : null;
}

function optionalInteger(value: string): number | null {
  if (!value.trim()) return null;
  const parsed = Number(value);
  return Number.isSafeInteger(parsed) ? parsed : null;
}

export function toPatientFindOrCreateRequest(
  data: PatientProfileFormData
): PatientFindOrCreateRequest {
  return {
    identificationCode: data.identificationCode || null,
    firstName: data.firstName || null,
    lastName: data.lastName || null,
    phone: data.phone || null,
    email: data.email || null,
    dateOfBirth: optionalDateTime(data.dateOfBirth),
    dateOfSurgery: optionalDateTime(data.dateOfSurgery),
    gender: data.gender as Gender,
    placeOfBirth: data.placeOfBirth || null,
    address: data.address || null,
    education: data.education as Education,
    occupation: data.occupation || null,
    fatherName: data.fatherName || null,
    height: optionalInteger(data.height),
    weight: optionalInteger(data.weight),
    referrer: data.referrer || null,
    insuranceProvider: data.insuranceProvider || null,
    visitReason: data.visitReason || null,
    allergies: data.allergies || null,
    currentMedications: data.currentMedications || null,
    clinicalNotes: data.clinicalNotes || null,
    emergencyContactName: data.emergencyContactName || null,
    emergencyContactPhone: data.emergencyContactPhone || null,
  };
}

export function useCreatePatientProfile() {
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: (data: PatientProfileFormData) =>
      clinicFindOrCreatePatient(toPatientFindOrCreateRequest(data)),
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
