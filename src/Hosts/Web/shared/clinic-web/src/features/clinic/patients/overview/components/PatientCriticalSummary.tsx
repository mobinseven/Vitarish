"use client";
import { DetailGrid, type DetailGridItem } from "@vitarish/web-ui";

import { Widget } from "@heroui-pro/react";
import type { PatientViewDto } from "../../../../../api/generated/clinic/model";
import { useLocalizedText } from "../../../../../localization";

interface PatientCriticalSummaryProps {
  patient: PatientViewDto;
}

export function PatientCriticalSummary({ patient }: PatientCriticalSummaryProps) {
  const { t } = useLocalizedText();
  const items = [
    patient.allergies?.trim()
      ? { label: t("clinic.patient.fields.allergies"), value: patient.allergies }
      : null,
    patient.currentMedications?.trim()
      ? { label: t("clinic.patient.fields.currentMedications"), value: patient.currentMedications }
      : null,
    patient.clinicalNotes?.trim()
      ? { label: t("clinic.patient.fields.clinicalNotes"), value: patient.clinicalNotes }
      : null,
  ].filter((item): item is { label: string; value: string } => item !== null);

  if (items.length === 0) return null;

  return (
    <Widget>
      <Widget.Header>
        <Widget.Title className="text-warning">{t("clinic.patient.clinicalSummary.title")}</Widget.Title>
      </Widget.Header>
      <Widget.Content>
        <DetailGrid items={items} className="flex flex-col gap-3" />
      </Widget.Content>
    </Widget>
  );
}