"use client";

import { Card } from "@heroui/react";
import type { PatientViewDto } from "@/api/generated/clinic/model";
import { useLocalizedText, formatDate as fmtDate, type SupportedLocale } from "@/localization";

interface PatientSummaryCardProps {
  patient: PatientViewDto;
}

function safeDate(dateString: string | null | undefined, locale: SupportedLocale): string {
  if (!dateString || dateString.startsWith("0001-01-01")) return "—";
  return fmtDate(dateString, locale) || "—";
}

export function PatientSummaryCard({ patient }: PatientSummaryCardProps) {
  const { t, locale } = useLocalizedText();
  const displayName =
    patient.firstName || patient.lastName
      ? ((patient.firstName ?? "") + " " + (patient.lastName ?? "")).trim()
      : patient.name || t("clinic.patients.unnamedPatient");

  return (
    <Card>
      <Card.Header>
        <h2 className="text-lg font-semibold">{displayName}</h2>
      </Card.Header>
      <Card.Content className="grid gap-3 sm:grid-cols-2">
        <SummaryField label={t("clinic.patient.fields.identificationCode")} value={patient.identificationCode} />
        <SummaryField label={t("clinic.patient.fields.phone")} value={patient.phone} />
        <SummaryField label={t("clinic.patient.fields.email")} value={patient.email} />
        <SummaryField label={t("clinic.patient.fields.dateOfBirth")} value={safeDate(patient.dateOfBirth, locale)} />
        <SummaryField label={t("clinic.patient.fields.gender")} value={patient.gender === "Unspecified" ? "—" : patient.gender} />
        <SummaryField label={t("clinic.patient.fields.insuranceProvider")} value={patient.insuranceProvider} />
      </Card.Content>
    </Card>
  );
}

function SummaryField({ label, value }: { label: string; value: string | null | undefined }) {
  const display = value && value.trim() ? value : "—";
  return (
    <div className="flex flex-col gap-1">
      <span className="text-xs text-muted">{label}</span>
      <span className="text-sm">{display}</span>
    </div>
  );
}
