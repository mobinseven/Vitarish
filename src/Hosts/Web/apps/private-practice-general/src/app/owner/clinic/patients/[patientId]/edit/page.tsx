"use client";

import { use, useCallback } from "react";
import { useRouter } from "next/navigation";
import { Card, Spinner } from "@heroui/react";
import { Routes } from "@/app/routes";
import { PatientProfileForm } from "@vitarish/clinic-web";
import { usePatientProfileEditor } from "@vitarish/clinic-web";
import { useLocalizedText } from "@/localization";

export default function EditPatientRoute({
  params,
}: {
  params: Promise<{ patientId: string }>;
}) {
  const { patientId } = use(params);
  const router = useRouter();
  const { t } = useLocalizedText();
  const { formData, isLoading, error, invalidatePatient } =
    usePatientProfileEditor(patientId);

  const handleSubmit = useCallback(
    async () => {
      invalidatePatient();
      router.push(Routes.Clinic.Patients.Detail(patientId));
    },
    [invalidatePatient, router, patientId]
  );

  const handleCancel = useCallback(() => {
    router.push(Routes.Clinic.Patients.Detail(patientId));
  }, [router, patientId]);

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

  if (error || !formData) {
    return (
      <Card className="p-8">
        <div className="flex flex-col items-center gap-4">
          <p className="text-danger">
            {error instanceof Error ? error.message : t("clinic.patient.detail.notFound")}
          </p>
        </div>
      </Card>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">{t("clinic.patient.edit.title")}</h1>
        <p className="text-sm text-muted">{t("clinic.patient.edit.description")}</p>
      </div>

      <PatientProfileForm
        initialData={formData}
        onSubmit={handleSubmit}
        onCancel={handleCancel}
      />
    </div>
  );
}
