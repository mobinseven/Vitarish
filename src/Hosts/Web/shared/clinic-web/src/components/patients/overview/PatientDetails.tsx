"use client";
import { DetailGrid, type DetailGridItem } from "@vitarish/web-ui";


import type { PatientViewDto } from "../../../models/patients/patientSearchModels";
import { useLocalizedText, formatDate as fmtDate, type SupportedLocale } from "../../../localization/index";

interface PatientDetailsProps {
  patient: PatientViewDto;
}

function safeDate(dateString: string | null | undefined, locale: SupportedLocale, fallback: string): string {
  if (!dateString || dateString.startsWith("0001-01-01")) return fallback;
  return fmtDate(dateString, locale) || fallback;
}

function formatEnum(value: string | null | undefined, fallback: string): string {
  if (!value || value === "Unspecified") return fallback;
  return value.replace(/([A-Z])/g, " $1").trim();
}

function displayValue(value: string | number | boolean | null | undefined, fallback: string) {
  return value === null || value === undefined || value === "" ? fallback : String(value);
}

export function PatientDetails({ patient }: PatientDetailsProps) {
  const { t, locale } = useLocalizedText();
  const empty = t("common.values.notAvailable");

  const sections: { title: string; items: DetailGridItem[] }[] = [
    {
      title: t("clinic.patient.sections.personal"),
      items: [
        { label: t("clinic.patient.fields.firstName"), value: displayValue(patient.firstName, empty) },
        { label: t("clinic.patient.fields.lastName"), value: displayValue(patient.lastName, empty) },
        { label: t("clinic.patient.fields.dateOfBirth"), value: safeDate(patient.dateOfBirth, locale, empty) },
        { label: t("clinic.patient.fields.gender"), value: formatEnum(patient.gender, empty) },
        { label: t("clinic.patient.fields.placeOfBirth"), value: displayValue(patient.placeOfBirth, empty) },
        { label: t("clinic.patient.fields.fatherName"), value: displayValue(patient.fatherName, empty) },
        { label: t("clinic.patient.fields.education"), value: formatEnum(patient.education, empty) },
        { label: t("clinic.patient.fields.occupation"), value: displayValue(patient.occupation, empty) },
      ],
    },
    {
      title: t("clinic.patient.sections.contact"),
      items: [
        { label: t("clinic.patient.fields.phone"), value: displayValue(patient.phone, empty) },
        { label: t("clinic.patient.fields.email"), value: displayValue(patient.email, empty) },
        { label: t("clinic.patient.fields.address"), value: displayValue(patient.address, empty) },
        { label: t("clinic.patient.fields.emergencyContact"), value: displayValue(patient.emergencyContactName, empty) },
        { label: t("clinic.patient.fields.emergencyPhone"), value: displayValue(patient.emergencyContactPhone, empty) },
      ],
    },
    {
      title: t("clinic.patient.sections.medical"),
      items: [
        { label: t("clinic.patient.fields.dateOfSurgery"), value: safeDate(patient.dateOfSurgery, locale, empty) },
        { label: t("clinic.patient.fields.heightCm"), value: displayValue(patient.height, empty) },
        { label: t("clinic.patient.fields.weightKg"), value: displayValue(patient.weight, empty) },
        { label: t("clinic.patient.fields.allergies"), value: displayValue(patient.allergies, empty) },
        { label: t("clinic.patient.fields.currentMedications"), value: displayValue(patient.currentMedications, empty) },
        { label: t("clinic.patient.fields.clinicalNotes"), value: displayValue(patient.clinicalNotes, empty) },
      ],
    },
    {
      title: t("clinic.patient.sections.insurance"),
      items: [
        { label: t("clinic.patient.fields.insuranceProvider"), value: displayValue(patient.insuranceProvider, empty) },
        { label: t("clinic.patient.fields.referrer"), value: displayValue(patient.referrer, empty) },
        { label: t("clinic.patient.fields.visitReason"), value: displayValue(patient.visitReason, empty) },
        { label: t("clinic.patient.fields.identificationCode"), value: displayValue(patient.identificationCode, empty) },
      ],
    },
    {
      title: t("clinic.patient.sections.system"),
      items: [
        { label: t("clinic.patient.fields.patientId"), value: displayValue(patient.id, empty) },
        { label: t("clinic.patient.fields.template"), value: patient.template ? t("clinic.patient.overview.yes") : t("clinic.patient.overview.no") },
        { label: t("clinic.patient.fields.created"), value: safeDate(patient.createdAt, locale, empty) },
        { label: t("clinic.patient.fields.updated"), value: safeDate(patient.updatedAt, locale, empty) },
      ],
    },
  ];

  return (
    <div className="flex flex-col gap-4">
      {sections.map((section) => (
        <section key={section.title} className="rounded-medium p-4">
          <header className="mb-3">
            <h2>{section.title}</h2>
          </header>
          <div>
            <DetailGrid items={section.items} />
          </div>
        </section>
      ))}
    </div>
  );
}
