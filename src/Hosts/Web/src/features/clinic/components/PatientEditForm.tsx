"use client";

import { Card, Button } from "@heroui/react";
import { useLocalizedText } from "@/localization";

interface PatientEditFormProps {
  patientId: string;
  onCancel: () => void;
}

export function PatientEditForm({ onCancel }: PatientEditFormProps) {
  const { t } = useLocalizedText();
  return (
    <Card>
      <Card.Header>
        <h2 className="text-lg font-semibold">{t("clinic.patient.edit.title")}</h2>
      </Card.Header>
      <Card.Content className="flex flex-col gap-4">
        <p className="text-default-500">
          {t("clinic.patient.edit.unavailable")}
        </p>
        <div className="flex justify-end gap-2">
          <Button variant="ghost" onPress={onCancel}>
            {t("clinic.patient.edit.cancel")}
          </Button>
        </div>
      </Card.Content>
    </Card>
  );
}
