"use client";

import { Card } from "@heroui/react";
import type { PatientViewDto } from "../types";
import { useLocalizedText, formatDate } from "@/localization";

interface PatientRowProps {
  patient: PatientViewDto;
  onSelect: (patientId: string) => void;
}

export function PatientRow({ patient, onSelect }: PatientRowProps) {
  const { t, locale } = useLocalizedText();
  return (
    <Card
      className="ds-surface-card ds-surface-card-default p-3 hover:bg-default cursor-pointer transition-colors"
    >
      <div
        className="flex items-center justify-between"
        onClick={() => patient.id && onSelect(patient.id)}
      >
        <div className="flex flex-col gap-1">
          <span className="font-medium">
            {patient.firstName || patient.lastName
              ? ((patient.firstName ?? "") + " " + (patient.lastName ?? "")).trim()
              : patient.name || t("clinic.patients.unnamedPatient")}
          </span>
          <span className="text-sm text-default-500">
            {patient.identificationCode && `ID: ${patient.identificationCode}`}
            {patient.phone && ` | ${patient.phone}`}
          </span>
        </div>
        <div className="text-sm text-default-500">
          {patient.dateOfBirth && formatDate(patient.dateOfBirth, locale)}
        </div>
      </div>
    </Card>
  );
}
