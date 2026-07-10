"use client";

import { useRouter } from "next/navigation";
import { Button } from "@heroui/react";
import { Routes } from "@/app/routes";
import { useLocalizedText } from "@/localization";

interface PatientQuickActionsProps {
  patientId: string;
}

export function PatientQuickActions({ patientId }: PatientQuickActionsProps) {
  const router = useRouter();
  const { t } = useLocalizedText();

  return (
    <div className="flex gap-2">
      <Button
        variant="ghost"
        onPress={() => router.push(Routes.Clinic.Patients.Edit(patientId))}
      >
        {t("clinic.patient.quickActions.edit")}
      </Button>
      <Button
        variant="ghost"
        onPress={() => router.push(Routes.Clinic.CheckIn)}
      >
        {t("clinic.patient.quickActions.checkIn")}
      </Button>
    </div>
  );
}
