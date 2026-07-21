"use client";

import { Card, Chip, Description, Label } from "@heroui/react";
import type { ReactNode } from "react";
import type { PatientViewDto } from "../../../models/patients/patientSearchModels";
import {
  formatDate,
  type SupportedLocale,
  useLocalizedText,
} from "../../../localization";

interface PatientProfileCardProps {
  patient: PatientViewDto;
}

interface PatientDetail {
  label: string;
  value: ReactNode;
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

export function PatientProfileCard({ patient }: PatientProfileCardProps) {
  const { t, locale } = useLocalizedText();
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
    { title: t("clinic.patient.sections.personal"), icon: "fa-user", items: personalDetails },
    { title: t("clinic.patient.sections.contact"), icon: "fa-phone", items: contactDetails },
    { title: t("clinic.patient.sections.medical"), icon: "fa-heart-pulse", items: careDetails },
  ].filter((section) => section.items.length > 0);

  if (sections.length === 0) return null;

  return (
    <div className="flex flex-wrap gap-4">
      {sections.map((section) => (
        <Card className="w-[200px] gap-2">
          <Card.Header>
            <span className="bg-surface shadow-surface flex size-8 items-center justify-center rounded-lg">
              <i className="fa-solid fa-gear"></i>
            </span>
            <div className="flex gap-2">
              <Label className="text-sm font-semibold">{section.title}</Label>

            </div>
          </Card.Header>
          <Card.Content>
            <Description className="mt-2">
              <DetailList items={section.items} /></Description>
          </Card.Content>
        </Card>
      ))}
    </div>
  );
}
