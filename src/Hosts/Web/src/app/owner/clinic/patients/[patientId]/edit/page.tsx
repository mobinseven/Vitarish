"use client";

import { use, useCallback } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@heroui/react";
import { Routes } from "@/app/routes";
import { PatientEditForm } from "@/features/clinic/components";

export default function EditPatientRoute({
  params,
}: {
  params: Promise<{ patientId: string }>;
}) {
  const { patientId } = use(params);
  const router = useRouter();

  const handleCancel = useCallback(() => {
    router.push(Routes.Clinic.Patients.Detail(patientId));
  }, [router, patientId]);

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Button
          variant="ghost"
          className="mb-2"
          onPress={() => router.push(Routes.Clinic.Patients.Detail(patientId))}
        >
          ← Back to Patient
        </Button>
        <h1 className="text-2xl font-bold">Edit Patient</h1>
      </div>

      <PatientEditForm patientId={patientId} onCancel={handleCancel} />
    </div>
  );
}
