"use client";

import { Button } from "@heroui/react";
import { Widget } from "@heroui-pro/react";
import { useLocalizedText } from "@/localization";

interface PatientEditFormProps {
  patientId: string;
  onCancel: () => void;
}

export function PatientEditForm({ onCancel }: PatientEditFormProps) {
  const { t } = useLocalizedText();
  return (
    <Widget>
      <Widget.Header>
        <Widget.Title>{t("clinic.patient.edit.title")}</Widget.Title>
      </Widget.Header>
      <Widget.Content className="flex flex-col gap-4">
        <p className="text-muted">
          {t("clinic.patient.edit.unavailable")}
        </p>
        <div className="flex justify-end gap-2">
          <Button variant="ghost" onPress={onCancel}>
            {t("clinic.patient.edit.cancel")}
          </Button>
        </div>
      </Widget.Content>
    </Widget>
  );
}
