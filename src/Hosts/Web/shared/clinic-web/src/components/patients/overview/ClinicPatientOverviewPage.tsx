"use client";

import type { ClinicNavigationIntent } from "../../../navigation";
import { PatientOverviewPageSurface } from "./PatientOverviewPageSurface";

export interface ClinicPatientOverviewPageProps {
  patientId: string;
  onNavigate: (intent: ClinicNavigationIntent) => void;
}

export function ClinicPatientOverviewPage({ patientId, onNavigate }: ClinicPatientOverviewPageProps) {
  return <PatientOverviewPageSurface patientId={patientId} onNavigate={onNavigate} />;
}
