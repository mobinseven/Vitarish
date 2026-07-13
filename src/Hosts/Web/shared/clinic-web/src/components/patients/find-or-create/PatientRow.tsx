"use client";

import { ItemCard, PressableFeedback } from "@heroui-pro/react";
import type { PatientViewDto } from "../../../models/patients/patientSearchModels";
import { useLocalizedText, formatDate } from "../../../localization/index";

interface PatientRowProps {
  patient: PatientViewDto;
  onSelect: (patientId: string) => void;
}

export function PatientRow({ patient, onSelect }: PatientRowProps) {
  const { t, locale } = useLocalizedText();
  const patientName = patient.firstName || patient.lastName
    ? ((patient.firstName ?? "") + " " + (patient.lastName ?? "")).trim()
    : patient.name || t("clinic.patients.unnamedPatient");
  const description = [
    patient.identificationCode
      ? `${t("clinic.patient.fields.identificationCode")}: ${patient.identificationCode}`
      : null,
    patient.phone,
  ].filter(Boolean).join(" / ");

  return (
    <ItemCard<"button">
      className="relative w-full cursor-pointer overflow-hidden text-start"
      render={(props) => (
        <button
          type="button"
          {...props}
          onClick={(event) => {
            props.onClick?.(event);
            if (patient.id) {
              onSelect(patient.id);
            }
          }}
        />
      )}
      variant="outline"
    >
      <PressableFeedback.Highlight />
      <ItemCard.Content>
        <ItemCard.Title>{patientName}</ItemCard.Title>
        {description && <ItemCard.Description>{description}</ItemCard.Description>}
      </ItemCard.Content>
      <ItemCard.Action>
        <span className="text-sm text-muted">
          {patient.dateOfBirth && formatDate(patient.dateOfBirth, locale)}
        </span>
      </ItemCard.Action>
    </ItemCard>
  );
}
