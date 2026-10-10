"use client";

import {
  formatDate,
  formatDateTime,
  formatNumber,
  useLocalizedText,
} from "../../localization";
import {
  asList,
  asNumber,
  asRecord,
  asText,
  parseRecord,
  type AssistantReviewPayload,
} from "./actionPayload";

interface ActionCardProps {
  payload: AssistantReviewPayload;
  expiresAt: string;
}

function ReviewField({
  label,
  value,
  clinicianAuthored = false,
}: {
  label: string;
  value: unknown;
  clinicianAuthored?: boolean;
}) {
  if (
    value == null ||
    value === "" ||
    (Array.isArray(value) && value.length === 0)
  )
    return null;
  const rendered = Array.isArray(value) ? value.join(", ") : String(value);
  return (
    <div
      className={
        clinicianAuthored
          ? "rounded-md border-s-4 border-muted bg-surface-secondary p-2"
          : "min-w-0"
      }
    >
      <dt className="text-xs font-semibold text-muted">{label}</dt>
      <dd className="break-words">{rendered}</dd>
    </div>
  );
}

export function AppointmentActionCard({ payload, expiresAt }: ActionCardProps) {
  const { t, locale } = useLocalizedText();
  const slot = asRecord(payload.slot);
  const date = asText(slot.date);
  const duration =
    asNumber(slot.durationMinutes) ?? asNumber(payload.durationMinutes);

  return (
    <dl className="grid grid-cols-1 gap-3 sm:grid-cols-2">
      <ReviewField
        label={t("clinic.clinicAssistant.fields.date")}
        value={date ? formatDate(date, locale) : null}
      />
      <ReviewField
        label={t("clinic.clinicAssistant.fields.startTime")}
        value={asText(slot.startTime)}
      />
      <ReviewField
        label={t("clinic.clinicAssistant.fields.duration")}
        value={
          duration == null
            ? asText(slot.duration)
            : formatNumber(duration, locale)
        }
      />
      <ReviewField
        label={t("clinic.clinicAssistant.fields.appointmentType")}
        value={asText(payload.appointmentType)}
      />
      <ReviewField
        label={t("clinic.clinicAssistant.fields.service")}
        value={asText(slot.service) ?? asText(payload.service)}
      />
      <ReviewField
        label={t("clinic.clinicAssistant.fields.practitioner")}
        value={asText(slot.practitioner) ?? asText(payload.practitioner)}
      />
      <ReviewField
        label={t("clinic.clinicAssistant.fields.description")}
        value={asText(payload.description)}
      />
      <ReviewField
        label={t("clinic.clinicAssistant.fields.warnings")}
        value={asList(payload.availabilityWarnings)}
      />
      <ReviewField
        label={t("clinic.clinicAssistant.fields.expiration")}
        value={formatDateTime(expiresAt, locale)}
      />
    </dl>
  );
}

export function PrescriptionActionCard({
  payload,
  expiresAt,
}: ActionCardProps) {
  const { t, locale } = useLocalizedText();
  const items = asList(payload.items).map(asRecord);
  return (
    <div className="space-y-3">
      <dl className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <ReviewField
          label={t("clinic.clinicAssistant.fields.template")}
          value={asText(payload.templateName) ?? asText(payload.templateId)}
        />
        <ReviewField
          label={t("clinic.clinicAssistant.fields.templateVersion")}
          value={asText(payload.templateVersion)}
        />
        <ReviewField
          label={t("clinic.clinicAssistant.fields.coverage")}
          value={asText(payload.insuranceCoverage)}
        />
        <ReviewField
          label={t("clinic.clinicAssistant.fields.warnings")}
          value={asList(payload.warnings)}
        />
        <ReviewField
          label={t("clinic.clinicAssistant.fields.expiration")}
          value={formatDateTime(expiresAt, locale)}
        />
      </dl>
      {items.map((item, index) => {
        const dosage = parseRecord(item.dosage ?? item.dosageJson);
        return (
          <dl
            key={asText(item.id) ?? index}
            className="grid grid-cols-1 gap-2 rounded-lg border border-border p-3 sm:grid-cols-2"
          >
            <ReviewField
              label={t("clinic.clinicAssistant.fields.medication")}
              value={asText(item.displayName) ?? asText(item.referenceCode)}
            />
            <ReviewField
              label={t("clinic.clinicAssistant.fields.dose")}
              value={asText(item.dose) ?? asText(dosage.dose)}
            />
            <ReviewField
              label={t("clinic.clinicAssistant.fields.frequency")}
              value={asText(item.frequency) ?? asText(dosage.frequency)}
            />
            <ReviewField
              label={t("clinic.clinicAssistant.fields.route")}
              value={asText(item.route) ?? asText(dosage.route)}
            />
            <ReviewField
              label={t("clinic.clinicAssistant.fields.duration")}
              value={asNumber(item.durationDays)}
            />
            <ReviewField
              label={t("clinic.clinicAssistant.fields.instructions")}
              value={asText(item.instructions) ?? asText(dosage.instructions)}
            />
          </dl>
        );
      })}
    </div>
  );
}

export function ClinicalEventActionCard({
  payload,
  expiresAt,
}: ActionCardProps) {
  const { t, locale } = useLocalizedText();
  const content = asRecord(payload.content);
  const performedAt = asText(content.performedAt);
  return (
    <dl className="grid grid-cols-1 gap-3 sm:grid-cols-2">
      <ReviewField
        label={t("clinic.clinicAssistant.fields.procedure")}
        value={asText(payload.procedureName) ?? asText(content.procedureId)}
      />
      <ReviewField
        label={t("clinic.clinicAssistant.fields.visitNumber")}
        value={asNumber(content.visitNumber)}
      />
      <ReviewField
        label={t("clinic.clinicAssistant.fields.eventType")}
        value={asText(content.eventType)}
      />
      <ReviewField
        label={t("clinic.clinicAssistant.fields.completionStatus")}
        value={asText(content.completionStatus)}
      />
      <ReviewField
        label={t("clinic.clinicAssistant.fields.performedAt")}
        value={performedAt ? formatDateTime(performedAt, locale) : null}
      />
      <ReviewField
        clinicianAuthored
        label={t("clinic.clinicAssistant.fields.observations")}
        value={asText(content.observations)}
      />
      <ReviewField
        clinicianAuthored
        label={t("clinic.clinicAssistant.fields.diagnosis")}
        value={asText(content.diagnosis)}
      />
      <ReviewField
        clinicianAuthored
        label={t("clinic.clinicAssistant.fields.plan")}
        value={asText(content.planOrFollowUp)}
      />
      <ReviewField
        label={t("clinic.clinicAssistant.fields.narrative")}
        value={asText(content.narrativeNote)}
      />
      <ReviewField
        label={t("clinic.clinicAssistant.fields.attachments")}
        value={asList(content.attachmentReferences)}
      />
      <ReviewField
        label={t("clinic.clinicAssistant.fields.warnings")}
        value={asList(payload.warnings)}
      />
      <ReviewField
        label={t("clinic.clinicAssistant.fields.expiration")}
        value={formatDateTime(expiresAt, locale)}
      />
    </dl>
  );
}

export function OperationsActionCard({ payload, expiresAt }: ActionCardProps) {
  const { t, locale } = useLocalizedText();
  const patient = asRecord(payload.patient);
  const appointment = asRecord(payload.appointment);
  const reschedule = asRecord(payload.reschedule);
  const shift = asRecord(payload.shift);
  const date = asText(appointment.date) ?? asText(reschedule.targetDate);
  const startTime = asText(shift.startTime);
  const endTime = asText(shift.endTime);
  const actionType = asText(payload.actionType);
  const actionLabels = {
    book_appointment: t("clinic.operationsAssistant.actions.bookAppointment"),
    reschedule_appointment: t("clinic.operationsAssistant.actions.rescheduleAppointment"),
    cancel_appointment: t("clinic.operationsAssistant.actions.cancelAppointment"),
    reject_appointment: t("clinic.operationsAssistant.actions.rejectAppointment"),
    create_shift: t("clinic.operationsAssistant.actions.createShift"),
    edit_shift: t("clinic.operationsAssistant.actions.editShift"),
    close_shift: t("clinic.operationsAssistant.actions.closeShift"),
    cancel_shift: t("clinic.operationsAssistant.actions.cancelShift"),
    archive_shift: t("clinic.operationsAssistant.actions.archiveShift"),
  } as const;
  const currentState = asText(payload.expectedAppointmentStatus) ?? asText(payload.expectedShiftStatus);
  const stateLabels = {
    Pending: t("clinic.operationsAssistant.states.pending"), Confirmed: t("clinic.operationsAssistant.states.confirmed"), Cancelled: t("clinic.operationsAssistant.states.cancelled"), Rejected: t("clinic.operationsAssistant.states.rejected"), Expired: t("clinic.operationsAssistant.states.expired"), Waitlisted: t("clinic.operationsAssistant.states.waitlisted"), Present: t("clinic.operationsAssistant.states.present"), Completed: t("clinic.operationsAssistant.states.completed"), Draft: t("clinic.operationsAssistant.states.draft"), Active: t("clinic.operationsAssistant.states.active"), Paused: t("clinic.operationsAssistant.states.paused"), Closed: t("clinic.operationsAssistant.states.closed"), Archived: t("clinic.operationsAssistant.states.archived"),
  } as const;

  return (
    <dl className="grid grid-cols-1 gap-3 sm:grid-cols-2">
      <ReviewField label={t("clinic.operationsAssistant.fields.patient")} value={asText(patient.displayName)} />
      <ReviewField label={t("clinic.operationsAssistant.fields.maskedIdentification")} value={asText(patient.maskedIdentification)} />
      <ReviewField label={t("clinic.operationsAssistant.fields.maskedPhone")} value={asText(patient.maskedPhone)} />
      <ReviewField label={t("clinic.operationsAssistant.fields.date")} value={date ? formatDate(date, locale) : null} />
      <ReviewField label={t("clinic.operationsAssistant.fields.time")} value={startTime && endTime ? `${startTime}–${endTime}` : startTime} />
      <ReviewField label={t("clinic.operationsAssistant.fields.currentState")} value={currentState && currentState in stateLabels ? stateLabels[currentState as keyof typeof stateLabels] : currentState} />
      <ReviewField label={t("clinic.operationsAssistant.fields.proposedState")} value={actionType && actionType in actionLabels ? actionLabels[actionType as keyof typeof actionLabels] : actionType} />
      <ReviewField label={t("clinic.operationsAssistant.fields.service")} value={asText(shift.serviceName) ?? asText(shift.serviceId)} />
      <ReviewField label={t("clinic.operationsAssistant.fields.provider")} value={asText(shift.serviceProviderName) ?? asText(shift.serviceProviderId)} />
      <ReviewField label={t("clinic.operationsAssistant.fields.timeZone")} value={asText(shift.timeZone)} />
      <ReviewField label={t("clinic.operationsAssistant.fields.expiration")} value={formatDateTime(expiresAt, locale)} />
    </dl>
  );
}
