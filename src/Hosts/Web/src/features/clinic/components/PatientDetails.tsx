"use client";

import { Card } from "@heroui/react";
import type { PatientViewDto } from "../types";
import { useLocalizedText, formatDate as fmtDate, type SupportedLocale } from "@/localization";

interface PatientDetailsProps {
  patient: PatientViewDto;
}

function safeDate(dateString: string | null | undefined, locale: SupportedLocale): string {
  if (!dateString || dateString.startsWith("0001-01-01")) return "—";
  return fmtDate(dateString, locale) || "—";
}

function formatEnum(value: string | null | undefined): string {
  if (!value || value === "Unspecified") return "—";
  return value.replace(/([A-Z])/g, " $1").trim();
}

export function PatientDetails({ patient }: PatientDetailsProps) {
  const { t, locale } = useLocalizedText();
  return (
    <div className="flex flex-col gap-4">
      <Card className="glass-card">
        <Card.Header>
          <h2 className="text-lg font-semibold">{t("clinic.patient.sections.personal")}</h2>
        </Card.Header>
        <Card.Content className="grid gap-4 sm:grid-cols-2">
          <DetailField label={t("clinic.patient.fields.firstName")} value={patient.firstName} />
          <DetailField label={t("clinic.patient.fields.lastName")} value={patient.lastName} />
          <DetailField
            label={t("clinic.patient.fields.dateOfBirth")}
            value={safeDate(patient.dateOfBirth, locale)}
          />
          <DetailField label={t("clinic.patient.fields.gender")} value={formatEnum(patient.gender)} />
          <DetailField label={t("clinic.patient.fields.placeOfBirth")} value={patient.placeOfBirth} />
          <DetailField label={t("clinic.patient.fields.fatherName")} value={patient.fatherName} />
          <DetailField label={t("clinic.patient.fields.education")} value={formatEnum(patient.education)} />
          <DetailField label={t("clinic.patient.fields.occupation")} value={patient.occupation} />
        </Card.Content>
      </Card>

      <Card className="glass-card">
        <Card.Header>
          <h2 className="text-lg font-semibold">{t("clinic.patient.sections.contact")}</h2>
        </Card.Header>
        <Card.Content className="grid gap-4 sm:grid-cols-2">
          <DetailField label={t("clinic.patient.fields.phone")} value={patient.phone} />
          <DetailField label={t("clinic.patient.fields.email")} value={patient.email} />
          <DetailField label={t("clinic.patient.fields.address")} value={patient.address} />
          <DetailField
            label={t("clinic.patient.fields.emergencyContact")}
            value={patient.emergencyContactName}
          />
          <DetailField
            label={t("clinic.patient.fields.emergencyPhone")}
            value={patient.emergencyContactPhone}
          />
        </Card.Content>
      </Card>

      <Card className="glass-card">
        <Card.Header>
          <h2 className="text-lg font-semibold">{t("clinic.patient.sections.medical")}</h2>
        </Card.Header>
        <Card.Content className="grid gap-4 sm:grid-cols-2">
          <DetailField
            label={t("clinic.patient.fields.dateOfSurgery")}
            value={safeDate(patient.dateOfSurgery, locale)}
          />
          <DetailField label={t("clinic.patient.fields.heightCm")} value={patient.height} />
          <DetailField label={t("clinic.patient.fields.weightKg")} value={patient.weight} />
          <DetailField label={t("clinic.patient.fields.allergies")} value={patient.allergies} />
          <DetailField
            label={t("clinic.patient.fields.currentMedications")}
            value={patient.currentMedications}
          />
          <DetailField label={t("clinic.patient.fields.clinicalNotes")} value={patient.clinicalNotes} />
        </Card.Content>
      </Card>

      <Card className="glass-card">
        <Card.Header>
          <h2 className="text-lg font-semibold">{t("clinic.patient.sections.insurance")}</h2>
        </Card.Header>
        <Card.Content className="grid gap-4 sm:grid-cols-2">
          <DetailField
            label={t("clinic.patient.fields.insuranceProvider")}
            value={patient.insuranceProvider}
          />
          <DetailField label={t("clinic.patient.fields.referrer")} value={patient.referrer} />
          <DetailField label={t("clinic.patient.fields.visitReason")} value={patient.visitReason} />
          <DetailField
            label={t("clinic.patient.fields.identificationCode")}
            value={patient.identificationCode}
          />
        </Card.Content>
      </Card>

      <Card className="glass-card">
        <Card.Header>
          <h2 className="text-lg font-semibold">{t("clinic.patient.sections.system")}</h2>
        </Card.Header>
        <Card.Content className="grid gap-4 sm:grid-cols-2">
          <DetailField label={t("clinic.patient.fields.patientId")} value={patient.id} />
          <DetailField label={t("clinic.patient.fields.template")} value={patient.template ? t("clinic.patient.overview.yes") : t("clinic.patient.overview.no")} />
          <DetailField label={t("clinic.patient.fields.created")} value={safeDate(patient.createdAt, locale)} />
          <DetailField label={t("clinic.patient.fields.updated")} value={safeDate(patient.updatedAt, locale)} />
        </Card.Content>
      </Card>
    </div>
  );
}

function DetailField({
  label,
  value,
}: {
  label: string;
  value: string | number | boolean | null | undefined;
}) {
  const displayValue =
    value === null || value === undefined || value === "" ? "—" : String(value);

  return (
    <div className="flex flex-col gap-1">
      <span className="text-sm text-default-500">{label}</span>
      <span className="text-sm">{displayValue}</span>
    </div>
  );
}
