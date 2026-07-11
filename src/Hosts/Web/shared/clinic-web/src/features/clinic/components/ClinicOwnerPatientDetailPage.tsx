"use client";

import { Button } from "@heroui/react";
import { ClinicLoadingState, ClinicErrorState } from "../../../features/clinic/components/ClinicStateSurface";
import { usePatientDetail } from "../hooks/usePatientDetail";
import { PatientDetails } from "./PatientDetails";
import { PatientOverviewPanel } from "./PatientOverviewPanel";
import { PatientQuickActions } from "./PatientQuickActions";
import { useLocalizedText } from "../../../localization";

interface ClinicOwnerPatientDetailPageProps {
  patientId: string;
  onNavigate?: (intent: import("../../../navigation").ClinicNavigationIntent) => void;
}

export function ClinicOwnerPatientDetailPage({ patientId, onNavigate }: ClinicOwnerPatientDetailPageProps) {
  const { t } = useLocalizedText();
  const { data: patient, isLoading, error } = usePatientDetail(patientId);

  if (isLoading) {
    return <ClinicLoadingState label={t("clinic.patient.detail.loading")} />;
  }

  if (error || !patient) {
    return (
      <ClinicErrorState title={error instanceof Error ? error.message : t("clinic.patient.detail.notFound")}>
        <Button variant="ghost" onPress={() => onNavigate?.({ kind: "patientIndex" })}>
          {t("clinic.patients.backToPatients")}
        </Button>
      </ClinicErrorState>
    );
  }

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
          <h1 className="text-2xl font-bold">
            {patient.firstName || patient.lastName
              ? ((patient.firstName ?? "") + " " + (patient.lastName ?? "")).trim()
              : patient.name || t("clinic.patients.unnamedPatient")}
          </h1>
        </div>
        <PatientQuickActions patientId={patientId} />
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        <div className="lg:col-span-2">
          <PatientDetails patient={patient} />
        </div>
        <div>
          <PatientOverviewPanel patient={patient} />
        </div>
      </div>
    </div>
  );
}
