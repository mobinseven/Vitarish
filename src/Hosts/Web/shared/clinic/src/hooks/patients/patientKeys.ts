import type { PatientSearchCriteria } from "../../models/patients/patientSearchModels";

export const patientKeys = {
  all: ["clinic", "patients"] as const,
  list: (criteria: PatientSearchCriteria) =>
    [...patientKeys.all, "list", criteria] as const,
  count: (search?: string, tags?: string[]) =>
    [...patientKeys.all, "count", search, tags] as const,
  detail: (patientId: string) =>
    [...patientKeys.all, "detail", patientId] as const,
  search: (criteria: PatientSearchCriteria) =>
    [...patientKeys.all, "search", criteria] as const,
};
