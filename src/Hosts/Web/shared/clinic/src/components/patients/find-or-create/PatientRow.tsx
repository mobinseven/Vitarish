"use client";

import { Chip } from "@heroui/react";
import { ItemCard, PressableFeedback } from "@heroui-pro/react";
import { FaIcon } from "@vitarish/ui";
import type {
  PatientSortBy,
  PatientViewDto,
} from "../../../models/patients/patientSearchModels";
import { patientTagClassName } from "../tags/patientTagColor";
import { useLocalizedText, formatDate } from "../../../localization/index";
import { resolvePatientSortValue } from "./patientSortValue";

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
  const sortValue = resolvePatientSortValue(subtitleField, patient, { locale });
  const description = sortValue
    ? `${t(sortValue.labelKey)}: ${sortValue.value}`
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
              <Chip key={tag} className={patientTagClassName(tag)} size="sm">
                {tag}
              </Chip>
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
