"use client";

import { useRouter } from "next/navigation";
import { Card, Spinner, Button } from "@heroui/react";
import { Routes } from "@/app/routes";
import { usePatientOverview } from "../hooks/usePatientOverview";
import { PatientSummaryCard } from "./PatientSummaryCard";
import { PatientCriticalSummary } from "./PatientCriticalSummary";
import { PatientQuickActions } from "./PatientQuickActions";
import { PatientDetails } from "@/features/clinic/components/PatientDetails";
import { PatientProceduresPanel } from "@/features/clinic/patients/procedures/components/PatientProceduresPanel";
import { useLocalizedText } from "@/localization";

interface PatientOverviewPageSurfaceProps {
  patientId: string;
}

export function PatientOverviewPageSurface({ patientId }: PatientOverviewPageSurfaceProps) {
  const router = useRouter();
  const { t } = useLocalizedText();
  const { data: patient, isLoading, error } = usePatientOverview(patientId);

  if (isLoading) {
    return (
      <Card className="p-8">
        <div className="flex items-center justify-center gap-2">
          <Spinner size="sm" />
          <span className="text-muted">{t("clinic.patient.detail.loading")}</span>
        </div>
      </Card>
    );
  }

  if (error || !patient) {
    return (
      <Card className="p-8">
        <div className="flex flex-col items-center gap-4">
          <p className="text-danger">
            {error instanceof Error ? error.message : t("clinic.patient.detail.notFound")}
          </p>
          <Button
            variant="ghost"
            onPress={() => router.push(Routes.Clinic.Patients.Index)}
          >
            {t("clinic.patients.backToPatients")}
          </Button>
        </div>
      </Card>
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
            onPress={() => router.push(Routes.Clinic.Patients.Index)}
          >
            ← {t("clinic.patients.backToPatients")}
          </Button>
          <h1 className="text-2xl font-bold">{displayName}</h1>
        </div>
        <PatientQuickActions patientId={patientId} />
      </div>

      <PatientCriticalSummary patient={patient} />

      <div className="grid gap-6 lg:grid-cols-3">
        <div className="lg:col-span-2">
          <PatientDetails patient={patient} />
        </div>
        <div className="flex flex-col gap-6">
          <PatientSummaryCard patient={patient} />
          <PatientProceduresPanel patientId={patientId} />
        </div>
      </div>
    </div>
  );
}
