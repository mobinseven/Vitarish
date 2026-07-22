"use client";

import { ItemCard, PressableFeedback } from "@heroui-pro/react";
import { FaIcon } from "@vitarish/web-ui";
import type { PatientViewDto } from "../../../models/patients/patientSearchModels";
import { useLocalizedText, formatDate } from "../../../localization/index";

interface PatientRowProps {
  patient: PatientViewDto;
  onSelect: (patientId: string) => void;
  variant?: "outline" | "secondary";
}

export function PatientRow({ patient, onSelect, variant = "outline" }: PatientRowProps) {
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
      variant={variant}
    >
      <PressableFeedback.Highlight />
      <ItemCard.Icon>
        <span
          aria-hidden="true"
          className="flex size-10 shrink-0 items-center justify-center rounded-full bg-accent/10 text-accent"
        >
          <FaIcon icon="user" />
        </span>
      </ItemCard.Icon>
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
