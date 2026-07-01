"use client";

import { useRouter } from "next/navigation";
import { Card, Button, Spinner } from "@heroui/react";
import { Routes } from "@/app/routes";
import { usePatientDetail } from "../hooks/usePatientDetail";
import { PatientDetails } from "./PatientDetails";
import { PatientOverviewPanel } from "./PatientOverviewPanel";
import { PatientQuickActions } from "./PatientQuickActions";
import { useLocalizedText } from "@/localization";

interface ClinicOwnerPatientDetailPageProps {
  patientId: string;
}

export function ClinicOwnerPatientDetailPage({
  patientId,
}: ClinicOwnerPatientDetailPageProps) {
  const router = useRouter();
  const { t } = useLocalizedText();
  const { data: patient, isLoading, error } = usePatientDetail(patientId);

  if (isLoading) {
    return (
      <Card className="p-8">
        <div className="flex items-center justify-center gap-2">
          <Spinner size="sm" />
          <span className="text-default-500">{t("clinic.patient.detail.loading")}</span>
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
