import type { SurgeryConflictProbe } from "../../models/surgeries/surgeryModels";

export const surgeryKeys = {
  all: ["clinic", "surgeries"] as const,
  calendar: (from: string, to: string) =>
    [...surgeryKeys.all, "calendar", from, to] as const,
  detail: (surgeryId: string) =>
    [...surgeryKeys.all, "detail", surgeryId] as const,
  patient: (patientId: string) =>
    [...surgeryKeys.all, "patient", patientId] as const,
  types: () => [...surgeryKeys.all, "types"] as const,
  settings: () => [...surgeryKeys.all, "settings"] as const,
  conflicts: (probe: SurgeryConflictProbe) =>
    [...surgeryKeys.all, "conflicts", probe] as const,
  suggestions: () => [...surgeryKeys.all, "suggestions"] as const,
  staff: () => [...surgeryKeys.all, "staff"] as const,
};
