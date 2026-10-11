"use client";

import { ItemCard, PressableFeedback } from "@heroui-pro/react";
import { FaIcon } from "@vitarish/ui";
import type {
  PatientSortBy,
  PatientViewDto,
} from "../../../models/patients/patientSearchModels";
import { useLocalizedText, formatDate } from "../../../localization/index";

interface PatientRowProps {
  patient: PatientViewDto;
  onSelect: (patientId: string) => void;
  subtitleField?: PatientSortBy;
  variant?: "default" | "outline" | "secondary";
}

export function PatientRow({
  patient,
  onSelect,
  subtitleField,
  variant = "outline",
}: PatientRowProps) {
  const { t, locale } = useLocalizedText();
  const patientName =
    patient.firstName || patient.lastName
      ? ((patient.firstName ?? "") + " " + (patient.lastName ?? "")).trim()
      : patient.name || t("clinic.patients.unnamedPatient");
  const description =
    subtitleField === "DateOfBirth"
      ? patient.dateOfBirth && formatDate(patient.dateOfBirth, locale)
      : subtitleField === "DateOfSurgery"
        ? patient.dateOfSurgery && formatDate(patient.dateOfSurgery, locale)
        : subtitleField === "Name"
          ? patientName
          : patient.identificationCode
            ? `${t("clinic.patient.fields.identificationCode")}: ${patient.identificationCode}`
            : null;

  return (
    <ItemCard<"button">
      className="relative w-full cursor-pointer overflow-hidden text-start transition-colors hover:bg-default"
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
      <ItemCard.Icon className="bg-surface-secondary">
        <FaIcon icon="user" />
      </ItemCard.Icon>
      <ItemCard.Content>
        <ItemCard.Title>{patientName}</ItemCard.Title>
        {description && (
          <ItemCard.Description>{description}</ItemCard.Description>
        )}
        {patient.tags?.length ? (
          <div className="mt-2 flex flex-wrap gap-1">
            {patient.tags.map((tag) => (
              <span key={tag} className="rounded-full bg-surface-secondary px-2 py-0.5 text-xs text-muted">
                {tag}
              </span>
            ))}
          </div>
        ) : null}
      </ItemCard.Content>
      <ItemCard.Action>
        <span className="text-sm text-muted">
          {patient.dateOfBirth && formatDate(patient.dateOfBirth, locale)}
        </span>
      </ItemCard.Action>
    </ItemCard>
  );
}
