"use client";

import { Card, Button } from "@heroui/react";
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
    return (
      <Card className="p-4">
        <div className="flex items-center justify-between">
          <div>
            <p className="font-medium">
              {patient.firstName} {patient.lastName}
            </p>
            <p className="text-sm text-muted">
              {patient.identificationCode && `ID: ${patient.identificationCode}`}
              {patient.phone && ` | ${patient.phone}`}
            </p>
          </div>
          <Button variant="primary" onPress={() => patient.id && onSelect(patient.id)}>
            {t("clinic.patient.identification.select")}
          </Button>
        </div>
      </Card>
    );
  }

  if (status === "not-found") {
    return (
      <Card className="p-4">
        <div className="flex items-center justify-between">
          <p className="text-muted">{t("clinic.patient.identification.notFound")}</p>
          <Button variant="ghost" onPress={onRetry}>
            {t("clinic.patient.identification.retry")}
          </Button>
        </div>
      </Card>
    );
  }

  if (status === "error") {
    return (
      <Card className="p-4">
        <div className="flex items-center justify-between">
          <p className="text-danger">{error ?? t("clinic.patient.identification.lookupFailed")}</p>
          <Button variant="ghost" onPress={onRetry}>
            {t("clinic.patient.identification.retry")}
          </Button>
        </div>
      </Card>
    );
  }

  return null;
}
