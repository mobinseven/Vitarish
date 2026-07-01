"use client";

import { useCallback } from "react";
import { useRouter } from "next/navigation";
import { Card, Spinner } from "@heroui/react";
import { Routes } from "@/app/routes";
import { PatientProfileForm } from "@/features/clinic/patients/profile/components/PatientProfileForm";
import { useCreatePatientProfile } from "@/features/clinic/patients/profile/hooks/useCreatePatientProfile";
import { emptyFormData } from "@/features/clinic/patients/profile/models/PatientProfileForm";
import { useLocalizedText } from "@/localization";
import type { PatientProfileFormData } from "@/features/clinic/patients/profile/models/PatientProfileForm";

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
        <p className="text-sm text-default-500">{t("clinic.patients.createPatient")}</p>
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
