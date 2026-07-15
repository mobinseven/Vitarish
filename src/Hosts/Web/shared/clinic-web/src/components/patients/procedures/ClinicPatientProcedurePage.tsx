"use client";

import type { ClinicNavigationIntent } from "../../../navigation";
import { PatientProcedureWorkspace } from "./PatientProcedureWorkspace";

export interface ClinicPatientProcedurePageProps {
  patientId: string;
  procedureId: string;
  onNavigate: (intent: ClinicNavigationIntent) => void;
}

export function ClinicPatientProcedurePage(props: ClinicPatientProcedurePageProps) {
  return <PatientProcedureWorkspace {...props} />;
}
