"use client";

import { Button } from "@heroui/react";
import {
  ErrorStateSurface as ClinicErrorState,
  LoadingStateSurface as ClinicLoadingState,
} from "@vitarish/web-ui";
import { usePatientDetail } from "../../../hooks/patients/usePatientDetail";
import { useLocalizedText } from "../../../localization";
import type { NavigationIntent } from "../../../navigation";
import { PatientProceduresPanel } from "../procedures/PatientProceduresPanel";
import { PatientOverviewCard } from "./PatientOverviewCard";

export interface PatientOverviewPageProps {
  patientId: string;
  onNavigate: (intent: NavigationIntent) => void;
}

export function PatientOverviewPage({ patientId, onNavigate }: PatientOverviewPageProps) {
  const { t } = useLocalizedText();
  const { data: patient, isLoading, error } = usePatientDetail(patientId);

  if (isLoading) {
    return <ClinicLoadingState label={t("clinic.patient.detail.loading")} />;
  }

  if (error || !patient) {
    return (
      <ClinicErrorState title={t("clinic.patient.detail.notFound")}>
        <Button variant="ghost" onPress={() => onNavigate({ kind: "patientIndex" })}>
          {t("clinic.patients.backToPatients")}
        </Button>
      </ClinicErrorState>
    );
  }

  return (
    <section className="flex flex-col gap-6" aria-label={t("clinic.patient.overview.title")}>
      <PatientOverviewCard
        patient={patient}
        onBack={() => onNavigate({ kind: "patientIndex" })}
        onCheckIn={() => onNavigate({ kind: "checkIn" })}
        onEdit={() => onNavigate({ kind: "patientEdit", patientId })}
      />
      <PatientProceduresPanel patientId={patientId} onNavigate={onNavigate} />
    </section>
  );
}
