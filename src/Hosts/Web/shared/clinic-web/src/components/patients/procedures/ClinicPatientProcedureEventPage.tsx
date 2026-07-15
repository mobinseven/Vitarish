"use client";

import type { ClinicNavigationIntent } from "../../../navigation";
import { PatientProcedureWorkspace } from "./PatientProcedureWorkspace";

export interface ClinicPatientProcedureEventPageProps {
  patientId: string;
  procedureId: string;
  eventId: string;
  onNavigate: (intent: ClinicNavigationIntent) => void;
}

export function ClinicPatientProcedureEventPage(props: ClinicPatientProcedureEventPageProps) {
  return <PatientProcedureWorkspace {...props} />;
}
