"use client";

import { Chip } from "@heroui/react";
import { usePatientProcedureTriage } from "../../../hooks/patients/procedures/usePatientProcedureTriage";
import { useLocalizedText } from "../../../localization/index";

interface PatientProcedureTriageSummaryProps {
  patientId: string;
}

export function PatientProcedureTriageSummary({ patientId }: PatientProcedureTriageSummaryProps) {
  const { t } = useLocalizedText();
  const { data: triage } = usePatientProcedureTriage(patientId);

  if (!triage) return null;

  const signals: { label: string; color: "warning" | "accent" | "danger" }[] = [];
  if (triage.incompleteProcedures > 0) {
    signals.push({ label: `${triage.incompleteProcedures} ${t("clinic.procedures.triage.incomplete")}`, color: "warning" });
  }
  if (triage.newEvidence > 0) {
    signals.push({ label: `${triage.newEvidence} ${t("clinic.procedures.triage.newEvidence")}`, color: "accent" });
  }
  if (triage.pendingReminders > 0) {
    signals.push({ label: `${triage.pendingReminders} ${t("clinic.procedures.triage.pendingReminders")}`, color: "danger" });
  }

  if (signals.length === 0) return null;

  return (
    <div className="flex flex-wrap gap-2">
      {signals.map((signal, index) => (
        <Chip key={index} size="sm" color={signal.color} variant="soft">
          {signal.label}
        </Chip>
      ))}
    </div>
  );
}
