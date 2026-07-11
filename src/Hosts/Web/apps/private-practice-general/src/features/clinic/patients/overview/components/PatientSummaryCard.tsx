"use client";

import { Widget } from "@heroui-pro/react";
import { ClinicDetailGrid } from "@vitarish/clinic-web";
import type { PatientViewDto } from "@/api/generated/clinic/model";
import { useLocalizedText, formatDate as fmtDate, type SupportedLocale } from "@/localization";

interface PatientSummaryCardProps {
  patient: PatientViewDto;
}

function safeDate(dateString: string | null | undefined, locale: SupportedLocale, fallback: string): string {
  if (!dateString || dateString.startsWith("0001-01-01")) return fallback;
  return fmtDate(dateString, locale) || fallback;
}

export function PatientSummaryCard({ patient }: PatientSummaryCardProps) {
  const { t, locale } = useLocalizedText();
  const empty = t("common.values.notAvailable");
  const displayName =
    patient.firstName || patient.lastName
      ? ((patient.firstName ?? "") + " " + (patient.lastName ?? "")).trim()
      : patient.name || t("clinic.patients.unnamedPatient");

  return (
    <Widget>
      <Widget.Header>
        <Widget.Title>{displayName}</Widget.Title>
      </Widget.Header>
      <Widget.Content>
        <ClinicDetailGrid
          items={[
            { label: t("clinic.patient.fields.identificationCode"), value: patient.identificationCode || empty },
            { label: t("clinic.patient.fields.phone"), value: patient.phone || empty },
            { label: t("clinic.patient.fields.email"), value: patient.email || empty },
            { label: t("clinic.patient.fields.dateOfBirth"), value: safeDate(patient.dateOfBirth, locale, empty) },
            { label: t("clinic.patient.fields.gender"), value: patient.gender === "Unspecified" ? empty : patient.gender || empty },
            { label: t("clinic.patient.fields.insuranceProvider"), value: patient.insuranceProvider || empty },
          ]}
        />
      </Widget.Content>
    </Widget>
  );
}
