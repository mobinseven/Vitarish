"use client";

import { useCallback } from "react";
import { useRouter } from "next/navigation";
import { Card } from "@heroui/react";
import { Routes } from "@/app/routes";
import { PatientProfileForm } from "@vitarish/clinic-web";
import { useCreatePatientProfile } from "@vitarish/clinic-web";
import { emptyFormData } from "@vitarish/clinic-web";
import { useLocalizedText } from "@/localization";
import type { PatientProfileFormData } from "@vitarish/clinic-web";

export default function NewPatientRoute() {
  const router = useRouter();
  const { t } = useLocalizedText();
  const { create, isPending, error } = useCreatePatientProfile();

  const handleSubmit = useCallback(
    async (data: PatientProfileFormData) => {
      try {
        const result = await create(data);
        if (result?.id) {
          router.push(Routes.Clinic.Patients.Detail(result.id));
        }
      } catch {
        // Error is surfaced through the hook's error state
      }
    },
    [create, router]
  );

  const handleCancel = useCallback(() => {
    router.push(Routes.Clinic.Patients.Index);
  }, [router]);

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">{t("clinic.patients.newPatient")}</h1>
        <p className="text-sm text-muted">{t("clinic.patients.createPatient")}</p>
      </div>

      {error && (
        <Card>
          <Card.Content className="py-4">
            <p className="text-sm text-danger">
              {error instanceof Error ? error.message : t("clinic.patient.edit.error")}
            </p>
          </Card.Content>
        </Card>
      )}

      <PatientProfileForm
        initialData={emptyFormData()}
        onSubmit={handleSubmit}
        onCancel={handleCancel}
        isPending={isPending}
      />
    </div>
  );
}
