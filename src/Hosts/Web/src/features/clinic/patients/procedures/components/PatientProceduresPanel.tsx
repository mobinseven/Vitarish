"use client";

import { Card } from "@heroui/react";
import { useLocalizedText } from "@/localization";

interface PatientProceduresPanelProps {
  patientId: string;
}

export function PatientProceduresPanel({ patientId: _patientId }: PatientProceduresPanelProps) {
  const { t } = useLocalizedText();
  return (
    <Card>
      <Card.Header>
        <h2 className="text-lg font-semibold">{t("clinic.procedures.title")}</h2>
      </Card.Header>
      <Card.Content>
        <p className="text-sm text-default-500">
          {t("clinic.procedures.comingSoon")}
        </p>
      </Card.Content>
    </Card>
  );
}
