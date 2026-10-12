"use client";

import type { ProcedureEventViewer } from "../../../models/patients/procedures/PatientProcedureEventItemModels";
import type { NavigationIntent } from "../../../navigation";
import { PatientProcedureWorkspace } from "./PatientProcedureWorkspace";

export interface PatientProcedurePageProps {
  patientId: string;
  procedureId: string;
  /** The signed-in member, so their own notes read as theirs. */
  viewer?: ProcedureEventViewer;
  onNavigate: (intent: NavigationIntent) => void;
}

export function PatientProcedurePage(props: PatientProcedurePageProps) {
  return <PatientProcedureWorkspace {...props} />;
}
