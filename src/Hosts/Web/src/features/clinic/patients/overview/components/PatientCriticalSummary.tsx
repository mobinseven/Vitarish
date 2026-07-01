"use client";

import { Card } from "@heroui/react";
import type { PatientViewDto } from "@/api/generated/clinic/model";
import { useLocalizedText } from "@/localization";

interface PatientCriticalSummaryProps {
  patient: PatientViewDto;
}

export function PatientCriticalSummary({ patient }: PatientCriticalSummaryProps) {
  const { t } = useLocalizedText();

  const hasCriticalData =
    patient.allergies?.trim() ||
    patient.currentMedications?.trim() ||
    patient.clinicalNotes?.trim();

  if (!hasCriticalData) return null;

  return (
    <Card>
      <Card.Header>
        <h2 className="text-lg font-semibold text-warning">{t("clinic.patient.clinicalSummary.title")}</h2>
      </Card.Header>
      <Card.Content className="flex flex-col gap-3">
        {patient.allergies?.trim() && (
          <div>
            <span className="text-xs text-default-500">{t("clinic.patient.fields.allergies")}</span>
            <p className="text-sm">{patient.allergies}</p>
          </div>
        )}
        {patient.currentMedications?.trim() && (
          <div>
            <span className="text-xs text-default-500">{t("clinic.patient.fields.currentMedications")}</span>
            <p className="text-sm">{patient.currentMedications}</p>
          </div>
        )}
        {patient.clinicalNotes?.trim() && (
          <div>
            <span className="text-xs text-default-500">{t("clinic.patient.fields.clinicalNotes")}</span>
            <p className="text-sm">{patient.clinicalNotes}</p>
          </div>
        )}
      </Card.Content>
    </Card>
  );
}
