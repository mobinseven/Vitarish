"use client";

import { Button } from "@heroui/react";
import { useLocalizedText } from "../../../../../localization";

interface PatientQuickActionsProps {
  patientId: string;
  onNavigate?: (intent: import("../../../../../navigation").ClinicNavigationIntent) => void;
}

export function PatientQuickActions({ patientId, onNavigate }: PatientQuickActionsProps) {
  const { t } = useLocalizedText();

  return (
    <div className="flex gap-2">
      <Button
        variant="ghost"
        onPress={() => onNavigate?.({ kind: "patientEdit", patientId: patientId })}
      >
        {t("clinic.patient.quickActions.edit")}
      </Button>
      <Button
        variant="ghost"
        onPress={() => onNavigate?.({ kind: "checkIn" })}
      >
        {t("clinic.patient.quickActions.checkIn")}
      </Button>
    </div>
  );
}
