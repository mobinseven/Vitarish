"use client";

import { Avatar, Button, Card, Separator } from "@heroui/react";
import type { ReactNode } from "react";
import type { PatientViewDto } from "../../../models/patients/patientSearchModels";
import {
  formatDate,
  type SupportedLocale,
  useLocalizedText,
} from "../../../localization";

interface PatientOverviewCardProps {
  patient: PatientViewDto;
  onBack: () => void;
  onCheckIn: () => void;
  onEdit: () => void;
}

interface PatientDetail {
  label: string;
  value: ReactNode;
}

function getDisplayName(patient: PatientViewDto, fallback: string): string {
  const name = [patient.firstName, patient.lastName].filter(Boolean).join(" ").trim();
  return name || patient.name?.trim() || fallback;
}

function getInitials(patient: PatientViewDto): string {
  const initials = [patient.firstName, patient.lastName]
    .filter(Boolean)
    .map((part) => part?.trim().charAt(0))
    .join("")
    .toLocaleUpperCase();

  return initials || patient.name?.trim().charAt(0).toLocaleUpperCase() || "?";
}

function formatPatientDate(value: string | null | undefined, locale: SupportedLocale): string | null {
  if (!value || value.startsWith("0001-01-01")) return null;
  return formatDate(value, locale) || null;
}

function formatEnum(value: string | null | undefined): string | null {
  if (!value || value === "Unspecified") return null;
  return value.replace(/([A-Z])/g, " $1").trim();
}

function detail(label: string, value: ReactNode): PatientDetail | null {
  return value === null || value === undefined || value === "" ? null : { label, value };
}

function DetailList({ items }: { items: PatientDetail[] }) {
  return (
    <dl className="grid min-w-0 gap-3">
      {items.map((item) => (
        <div key={item.label} className="min-w-0">
          <dt className="text-xs font-medium text-muted">{item.label}</dt>
          <dd className="mt-0.5 break-words text-sm text-foreground">{item.value}</dd>
        </div>
      ))}
    </dl>
  );
}

export function PatientOverviewCard({ patient, onBack, onCheckIn, onEdit }: PatientOverviewCardProps) {
  const { t, locale } = useLocalizedText();
  const displayName = getDisplayName(patient, t("clinic.patients.unnamedPatient"));
  const personalDetails = [
    detail(t("clinic.patient.fields.dateOfBirth"), formatPatientDate(patient.dateOfBirth, locale)),
    detail(t("clinic.patient.fields.gender"), formatEnum(patient.gender)),
  ].filter((item): item is PatientDetail => item !== null);
  const contactDetails = [
    detail(t("clinic.patient.fields.phone"), patient.phone),
    detail(t("clinic.patient.fields.email"), patient.email),
    detail(t("clinic.patient.fields.address"), patient.address),
  ].filter((item): item is PatientDetail => item !== null);
  const careDetails = [
    detail(t("clinic.patient.fields.insuranceProvider"), patient.insuranceProvider),
    detail(t("clinic.patient.fields.allergies"), patient.allergies),
    detail(t("clinic.patient.fields.currentMedications"), patient.currentMedications),
  ].filter((item): item is PatientDetail => item !== null);
  const sections = [
    { title: t("clinic.patient.sections.personal"), items: personalDetails },
    { title: t("clinic.patient.sections.contact"), items: contactDetails },
    { title: t("clinic.patient.sections.medical"), items: careDetails },
  ].filter((section) => section.items.length > 0);

  return (
    <Card className="overflow-hidden" variant="secondary">
      <Card.Header className="flex flex-col items-start gap-4 p-2 sm:flex-row sm:items-center">
        <Avatar color="accent" size="lg" variant="soft" role="img" aria-label={displayName}>
          <Avatar.Fallback className="text-lg font-semibold">
            <i className="fa-solid fa-user" />
          </Avatar.Fallback>
        </Avatar>
        <div className="min-w-0 flex-1">
          <h1 className="truncate text-xl font-semibold text-foreground sm:text-2xl">{displayName}</h1>
          {patient.identificationCode ? (
            <Card.Description className="mt-1 break-all">
              {t("clinic.patient.fields.identificationCode")}: {patient.identificationCode}
            </Card.Description>
          ) : null}
        </div>
      </Card.Header>

      {sections.length > 0 ? (
        <>
          <Card.Content className="grid gap-1 p-2 sm:grid-cols-2 xl:grid-cols-3">
            {sections.map((section) => (
              <section key={section.title} className="min-w-0">
                <h2 className="mb-3 text-sm font-semibold text-foreground">
                  {section.title}
                </h2>
                <DetailList items={section.items} />
              </section>
            ))}
          </Card.Content>
        </>
      ) : null}

      <Card.Footer className="flex flex-wrap justify-end gap-1 p-2 sm:p-5">
        <Button variant="ghost" onPress={onBack}>
          {t("clinic.patients.backToPatients")}
        </Button>
        <Button variant="secondary" onPress={onEdit}>
          {t("clinic.patient.quickActions.edit")}
        </Button>
        <Button variant="primary" onPress={onCheckIn}>
          {t("clinic.patient.quickActions.checkIn")}
        </Button>
      </Card.Footer>
    </Card>
  );
}
