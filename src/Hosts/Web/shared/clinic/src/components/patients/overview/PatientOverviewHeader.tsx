"use client";

import { Avatar, Card } from "@heroui/react";
import { FaIcon, RevealHeader } from "@vitarish/ui";
import type { PatientViewDto } from "../../../models/patients/patientSearchModels";
import { formatDate, type SupportedLocale } from "../../../localization";
import { useLocalizedText } from "../../../localization";

interface PatientOverviewHeaderProps {
  patient: PatientViewDto;
}

function getDisplayName(patient: PatientViewDto, fallback: string): string {
  const name = [patient.firstName, patient.lastName]
    .filter(Boolean)
    .join(" ")
    .trim();
  return name || patient.name?.trim() || fallback;
}

function getInitials(displayName: string): string {
  return displayName
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((name) => name[0]?.toUpperCase())
    .join("");
}

function formatPatientDate(
  value: string | null | undefined,
  locale: SupportedLocale,
): string | null {
  if (!value || value.startsWith("0001-01-01")) return null;
  return formatDate(value, locale) || null;
}

function formatEnum(value: string | null | undefined): string | null {
  if (!value || value === "Unspecified") return null;
  return value.replace(/([A-Z])/g, " $1").trim();
}

export function PatientOverviewHeader({ patient }: PatientOverviewHeaderProps) {
  const { t, locale } = useLocalizedText();
  const displayName = getDisplayName(
    patient,
    t("clinic.patients.unnamedPatient"),
  );
  const initials = getInitials(displayName);
  const dateOfBirth = formatPatientDate(patient.dateOfBirth, locale);
  const gender = formatEnum(patient.gender);

  return (
    <Card className="rounded-3xl bg-anchor text-anchor-foreground">
      <Card.Header className="flex flex-col items-center gap-4 px-4 py-6 text-center sm:flex-row sm:px-6 sm:text-left">
        <Avatar
          color="default"
          size="lg"
          variant="soft"
          role="img"
          aria-label={displayName}
        >
          <Avatar.Fallback className="bg-anchor-foreground/15 text-lg font-semibold text-anchor-foreground">
            <FaIcon icon="user" />
          </Avatar.Fallback>
        </Avatar>
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center justify-center gap-2 sm:justify-start">
            <h1 className="text-2xl font-semibold text-anchor-foreground sm:text-3xl">
              {displayName}
            </h1>
          </div>
          <div className="mt-2 flex flex-wrap justify-center gap-x-3 gap-y-1 text-sm text-anchor-foreground/75 sm:justify-start">
            {patient.identificationCode ? (
              <span>
                {t("clinic.patient.fields.identificationCode")}:{" "}
                {patient.identificationCode}
              </span>
            ) : null}
            {dateOfBirth ? <span>{dateOfBirth}</span> : null}
            {gender ? <span>{gender}</span> : null}
          </div>
        </div>
      </Card.Header>
      <RevealHeader
        compactContent={
          <div className="flex min-h-14 items-center gap-3 border-b border-border bg-background/95 px-4 shadow-sm backdrop-blur">
            <Avatar
              color="default"
              size="sm"
              variant="soft"
              role="img"
              aria-label={displayName}
            >
              <Avatar.Fallback>
                <FaIcon icon="user" />
              </Avatar.Fallback>
            </Avatar>
            <div className="min-w-0">
              <p className="truncate font-semibold text-foreground">
                {displayName}
              </p>
              {patient.identificationCode ? (
                <p className="truncate text-sm text-muted">
                  {patient.identificationCode}
                </p>
              ) : null}
            </div>
          </div>
        }
      />
      <RevealHeader.Boundary />
    </Card>
  );
}
