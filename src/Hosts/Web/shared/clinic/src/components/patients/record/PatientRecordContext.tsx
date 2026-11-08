"use client";

import { createContext, useContext } from "react";
import type { NavigationIntent } from "../../../navigation";
import type { PatientViewDto } from "../../../models/patients/patientSearchModels";

export interface PatientRecordContextValue {
  patientId: string;
  patient: PatientViewDto;
  onNavigate: (intent: NavigationIntent) => void;
  /** The patient's visits procedure, once it exists; starting a visit needs it. */
  visitsProcedureId: string | undefined;
  openNewVisit: () => void;
  openPrescription: () => void;
}

const PatientRecordContext = createContext<PatientRecordContextValue | null>(null);

export const PatientRecordProvider = PatientRecordContext.Provider;

/** What the record's shell has already loaded, for the tab inside it. */
export function usePatientRecord(): PatientRecordContextValue {
  const value = useContext(PatientRecordContext);
  if (!value) throw new Error("usePatientRecord must be used inside the patient record.");
  return value;
}
