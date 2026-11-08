/** The tabs of a patient's record, in display order; the first is the default. */
export const patientRecordTabs = ["overview", "visits", "surgeries", "procedures", "prescriptions"] as const;

export type PatientRecordTab = (typeof patientRecordTabs)[number];

export const defaultPatientRecordTab: PatientRecordTab = patientRecordTabs[0];

export function toPatientRecordTab(value: string | null | undefined): PatientRecordTab {
  return patientRecordTabs.find((tab) => tab === value) ?? defaultPatientRecordTab;
}
