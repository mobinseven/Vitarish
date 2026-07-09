"use client";

import { Button } from "@heroui/react";
import { ItemCard } from "@heroui-pro/react";
import { ClinicEmptyState, ClinicErrorState } from "./ClinicStateSurface";
import type { PatientViewDto } from "../types";
import { useLocalizedText } from "@/localization";

interface IdentificationLookupResultProps {
  status: "found" | "not-found" | "error";
  patient: PatientViewDto | null;
  error: string | null;
  onSelect: (patientId: string) => void;
  onRetry: () => void;
}

export function IdentificationLookupResult({
  status,
  patient,
  error,
  onSelect,
  onRetry,
}: IdentificationLookupResultProps) {
  const { t } = useLocalizedText();

  if (status === "found" && patient) {
    const description = [
      patient.identificationCode
        ? `${t("clinic.patient.fields.identificationCode")}: ${patient.identificationCode}`
        : null,
      patient.phone,
    ].filter(Boolean).join(" / ");

    return (
      <ItemCard variant="outline">
        <ItemCard.Content>
          <ItemCard.Title>{patient.firstName} {patient.lastName}</ItemCard.Title>
          {description && <ItemCard.Description>{description}</ItemCard.Description>}
        </ItemCard.Content>
        <ItemCard.Action>
          <Button variant="primary" onPress={() => patient.id && onSelect(patient.id)}>
            {t("clinic.patient.identification.select")}
          </Button>
        </ItemCard.Action>
      </ItemCard>
    );
  }

  if (status === "not-found") {
    return (
      <ClinicEmptyState title={t("clinic.patient.identification.notFound")}>
        <div className="flex justify-center">
          <Button variant="ghost" onPress={onRetry}>
            {t("clinic.patient.identification.retry")}
          </Button>
        </div>
      </ClinicEmptyState>
    );
  }

  if (status === "error") {
    return (
      <ClinicErrorState title={error ?? t("clinic.patient.identification.lookupFailed")}>
        <div className="flex justify-center">
          <Button variant="ghost" onPress={onRetry}>
            {t("clinic.patient.identification.retry")}
          </Button>
        </div>
      </ClinicErrorState>
    );
  }

  return null;
}
