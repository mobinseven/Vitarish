"use client";

import { formatDate, formatDateTime, formatNumber, useLocalizedText } from "../../localization";
import { asList, asNumber, asRecord, asText, parseRecord, type PatientAgentReviewPayload } from "./actionPayload";

interface ActionCardProps {
  payload: PatientAgentReviewPayload;
  expiresAt: string;
}

function ReviewField({ label, value, clinicianAuthored = false }: { label: string; value: unknown; clinicianAuthored?: boolean }) {
  if (value == null || value === "" || (Array.isArray(value) && value.length === 0)) return null;
  const rendered = Array.isArray(value) ? value.join(", ") : String(value);
  return (
    <div className={clinicianAuthored ? "rounded-md border-s-4 border-primary bg-primary/5 p-2" : "min-w-0"}>
      <dt className="text-xs font-semibold text-muted">{label}</dt>
      <dd className="break-words">{rendered}</dd>
    </div>
  );
}

export function AppointmentActionCard({ payload, expiresAt }: ActionCardProps) {
  const { t, locale } = useLocalizedText();
  const slot = asRecord(payload.slot);
  const date = asText(slot.date);
  const duration = asNumber(slot.durationMinutes) ?? asNumber(payload.durationMinutes);

  return (
    <dl className="grid grid-cols-1 gap-3 sm:grid-cols-2">
      <ReviewField label={t("clinic.patientAgent.fields.date")} value={date ? formatDate(date, locale) : null} />
      <ReviewField label={t("clinic.patientAgent.fields.startTime")} value={asText(slot.startTime)} />
      <ReviewField label={t("clinic.patientAgent.fields.duration")} value={duration == null ? asText(slot.duration) : formatNumber(duration, locale)} />
      <ReviewField label={t("clinic.patientAgent.fields.appointmentType")} value={asText(payload.appointmentType)} />
      <ReviewField label={t("clinic.patientAgent.fields.service")} value={asText(slot.service) ?? asText(payload.service)} />
      <ReviewField label={t("clinic.patientAgent.fields.practitioner")} value={asText(slot.practitioner) ?? asText(payload.practitioner)} />
      <ReviewField label={t("clinic.patientAgent.fields.description")} value={asText(payload.description)} />
      <ReviewField label={t("clinic.patientAgent.fields.warnings")} value={asList(payload.availabilityWarnings)} />
      <ReviewField label={t("clinic.patientAgent.fields.expiration")} value={formatDateTime(expiresAt, locale)} />
    </dl>
  );
}

export function PrescriptionActionCard({ payload, expiresAt }: ActionCardProps) {
  const { t, locale } = useLocalizedText();
  const items = asList(payload.items).map(asRecord);
  return (
    <div className="space-y-3">
      <dl className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <ReviewField label={t("clinic.patientAgent.fields.template")} value={asText(payload.templateName) ?? asText(payload.templateId)} />
        <ReviewField label={t("clinic.patientAgent.fields.templateVersion")} value={asText(payload.templateVersion)} />
        <ReviewField label={t("clinic.patientAgent.fields.coverage")} value={asText(payload.insuranceCoverage)} />
        <ReviewField label={t("clinic.patientAgent.fields.warnings")} value={asList(payload.warnings)} />
        <ReviewField label={t("clinic.patientAgent.fields.expiration")} value={formatDateTime(expiresAt, locale)} />
      </dl>
      {items.map((item, index) => {
        const dosage = parseRecord(item.dosage ?? item.dosageJson);
        return (
          <dl key={asText(item.id) ?? index} className="grid grid-cols-1 gap-2 rounded-lg border border-border p-3 sm:grid-cols-2">
            <ReviewField label={t("clinic.patientAgent.fields.medication")} value={asText(item.displayName) ?? asText(item.referenceCode)} />
            <ReviewField label={t("clinic.patientAgent.fields.dose")} value={asText(item.dose) ?? asText(dosage.dose)} />
            <ReviewField label={t("clinic.patientAgent.fields.frequency")} value={asText(item.frequency) ?? asText(dosage.frequency)} />
            <ReviewField label={t("clinic.patientAgent.fields.route")} value={asText(item.route) ?? asText(dosage.route)} />
            <ReviewField label={t("clinic.patientAgent.fields.duration")} value={asNumber(item.durationDays)} />
            <ReviewField label={t("clinic.patientAgent.fields.instructions")} value={asText(item.instructions) ?? asText(dosage.instructions)} />
          </dl>
        );
      })}
    </div>
  );
}

export function ClinicalEventActionCard({ payload, expiresAt }: ActionCardProps) {
  const { t, locale } = useLocalizedText();
  const content = asRecord(payload.content);
  const performedAt = asText(content.performedAt);
  return (
    <dl className="grid grid-cols-1 gap-3 sm:grid-cols-2">
      <ReviewField label={t("clinic.patientAgent.fields.procedure")} value={asText(payload.procedureName) ?? asText(content.procedureId)} />
      <ReviewField label={t("clinic.patientAgent.fields.visitNumber")} value={asNumber(content.visitNumber)} />
      <ReviewField label={t("clinic.patientAgent.fields.eventType")} value={asText(content.eventType)} />
      <ReviewField label={t("clinic.patientAgent.fields.completionStatus")} value={asText(content.completionStatus)} />
      <ReviewField label={t("clinic.patientAgent.fields.performedAt")} value={performedAt ? formatDateTime(performedAt, locale) : null} />
      <ReviewField clinicianAuthored label={t("clinic.patientAgent.fields.observations")} value={asText(content.observations)} />
      <ReviewField clinicianAuthored label={t("clinic.patientAgent.fields.diagnosis")} value={asText(content.diagnosis)} />
      <ReviewField clinicianAuthored label={t("clinic.patientAgent.fields.plan")} value={asText(content.planOrFollowUp)} />
      <ReviewField label={t("clinic.patientAgent.fields.narrative")} value={asText(content.narrativeNote)} />
      <ReviewField label={t("clinic.patientAgent.fields.attachments")} value={asList(content.attachmentReferences)} />
      <ReviewField label={t("clinic.patientAgent.fields.warnings")} value={asList(payload.warnings)} />
      <ReviewField label={t("clinic.patientAgent.fields.expiration")} value={formatDateTime(expiresAt, locale)} />
    </dl>
  );
}
