"use client";

import type { NavigationIntent } from "../../../navigation";
import { PatientProcedureWorkspace } from "./PatientProcedureWorkspace";

export interface PatientProcedurePageProps {
  patientId: string;
  procedureId: string;
  onNavigate: (intent: NavigationIntent) => void;
}

export function PatientProcedurePage(props: PatientProcedurePageProps) {
  return <PatientProcedureWorkspace {...props} />;
}
