"use client";

import { Button } from "@heroui/react";
import { LoadingStateSurface as ClinicLoadingState, ErrorStateSurface as ClinicErrorState } from "@vitarish/web-ui";
import { usePatientOverview } from "../hooks/usePatientOverview";
import { PatientSummary } from "./PatientSummary";
import { PatientCriticalSummary } from "./PatientCriticalSummary";
import { PatientQuickActions } from "./PatientQuickActions";
import { PatientDetails } from "./PatientDetails";
import { PatientProceduresPanel } from "../../../../../features/clinic/patients/procedures/components/PatientProceduresPanel";
import { useLocalizedText } from "../../../../../localization";

interface PatientOverviewPageSurfaceProps {
  patientId: string;
  onNavigate?: (intent: import("../../../../../navigation").ClinicNavigationIntent) => void;
}

export function PatientOverviewPageSurface({ patientId, onNavigate }: PatientOverviewPageSurfaceProps) {
  const { t } = useLocalizedText();
  const { data: patient, isLoading, error } = usePatientOverview(patientId);

  if (isLoading) {
    return <ClinicLoadingState label={t("clinic.patient.detail.loading")} />;
  }

  if (error || !patient) {
    return (
      <ClinicErrorState title={t("clinic.patient.detail.notFound")}>
        <Button variant="ghost" onPress={() => onNavigate?.({ kind: "patientIndex" })}>
          {t("clinic.patients.backToPatients")}
        </Button>
      </ClinicErrorState>
    );
  }

  const displayName =
    patient.firstName || patient.lastName
      ? ((patient.firstName ?? "") + " " + (patient.lastName ?? "")).trim()
      : patient.name || t("clinic.patients.unnamedPatient");

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <div>
          <Button
            variant="ghost"
            className="mb-2"
            onPress={() => onNavigate?.({ kind: "patientIndex" })}
          >
            ← {t("clinic.patients.backToPatients")}
          </Button>
          <h1 className="text-2xl font-bold">{displayName}</h1>
        </div>
        <PatientQuickActions patientId={patientId} onNavigate={onNavigate} />
      </div>

      <PatientCriticalSummary patient={patient} />

      <div className="grid gap-6 lg:grid-cols-3">
        <div className="lg:col-span-2">
          <PatientDetails patient={patient} />
        </div>
        <div className="flex flex-col gap-6">
          <PatientSummary patient={patient} />
          <PatientProceduresPanel patientId={patientId} onNavigate={onNavigate} />
        </div>
      </div>
    </div>
  );
}
