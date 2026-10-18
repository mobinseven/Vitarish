"use client";

import { Button, Chip, Tooltip } from "@heroui/react";
import { FaIcon } from "@vitarish/ui";
import type { NavigationIntent } from "../../../navigation";
import { formatDate, useLocalizedText } from "../../../localization";
import {
  visitPatientId,
  visitPatientLabel,
  visitServiceLabel,
  visitStatusLabel,
  type Visit,
} from "../../../models/appointments/visitRows";
import type { PatientQuickAction } from "../../patients/overview/PatientOverviewPage";
import { patientTagClassName } from "../../patients/tags/patientTagColor";
import { reservationStatusChipProps } from "../reservations/reservationStatusChip";

export interface VisitListProps {
  visits: Visit[];
  /** Show each visit's date above its time, for periods longer than a day. */
  showDate?: boolean;
  onNavigate: (intent: NavigationIntent) => void;
}

// The list adapts to its own width: a half-width dashboard panel drops the
// service column and shrinks the row action to its icon.
const rowColumns =
  "grid-cols-[5rem_minmax(0,1fr)_auto_auto] @2xl:grid-cols-[6rem_minmax(0,1.4fr)_minmax(0,1fr)_minmax(0,0.8fr)_auto]";

/**
 * Visits as rows of time, patient with tags, service, and status. Pressing a
 * row opens the patient; each row keeps one action, a new note.
 */
export function VisitList({ visits, showDate = false, onNavigate }: VisitListProps) {
  const { t, locale } = useLocalizedText();

  function openPatient(visit: Visit, quickAction?: PatientQuickAction) {
    const patientId = visitPatientId(visit);
    onNavigate(patientId ? { kind: "patientDetail", patientId, quickAction } : { kind: "patientIndex" });
  }

  return (
    <div className="@container">
      <div
        aria-hidden="true"
        className={`hidden gap-3 border-b border-separator bg-surface-secondary px-4 py-3 text-xs font-semibold text-foreground sm:px-6 @2xl:grid ${rowColumns}`}
      >
        <span>{t("clinic.appointments.visits.timeColumn")}</span>
        <span>{t("clinic.appointments.visits.patientColumn")}</span>
        <span>{t("clinic.appointments.visits.serviceColumn")}</span>
        <span>{t("clinic.appointments.visits.statusColumn")}</span>
        <span>{t("clinic.appointments.visits.actionsColumn")}</span>
      </div>
      <ul className="divide-y divide-separator">
        {visits.map((visit) => {
          const patient = visitPatientLabel(visit, t);
          const status = visitStatusLabel(visit.status, t);
          const chip = reservationStatusChipProps(visit.status);
          const newNoteLabel = t("clinic.appointments.visits.newNoteFor", { patient });

          return (
            <li
              key={visit.reservationId}
              className={`relative grid min-h-16 items-center gap-3 px-4 py-3 transition-colors hover:bg-surface-secondary sm:px-6 ${rowColumns}`}
            >
              <span className="flex flex-col text-sm">
                {showDate ? (
                  <span className="text-muted">
                    {formatDate(`${visit.date}T00:00:00`, locale, { weekday: "short", day: "numeric", month: "short" })}
                  </span>
                ) : null}
                <span className="font-semibold tabular-nums text-foreground">
                  {visit.startTime
                    ? formatDate(`${visit.date}T${visit.startTime}`, locale, { hour: "numeric", minute: "2-digit" })
                    : t("clinic.appointments.visits.noTime")}
                </span>
              </span>
              <span className="flex min-w-0 flex-col gap-1">
                {/* The patient name stretches over the row, so pressing anywhere opens the patient. */}
                <button
                  type="button"
                  aria-label={t("clinic.appointments.visits.openPatient", { patient })}
                  className="truncate rounded-sm text-start text-sm font-semibold text-foreground after:absolute after:inset-0 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
                  onClick={() => openPatient(visit)}
                >
                  {patient}
                </button>
                {visit.patientTags.length > 0 ? (
                  <ul aria-label={t("clinic.appointments.visits.patientTagsLabel")} className="flex flex-wrap gap-1">
                    {visit.patientTags.map((tag) => (
                      <li key={tag}>
                        <Chip className={patientTagClassName(tag)} size="sm">
                          <Chip.Label>{tag}</Chip.Label>
                        </Chip>
                      </li>
                    ))}
                  </ul>
                ) : null}
              </span>
              <span className="hidden min-w-0 truncate text-sm text-foreground @2xl:block">
                {visitServiceLabel(visit, locale, t)}
              </span>
              <Chip {...chip} className={`${chip.className ?? ""} w-fit whitespace-nowrap`} size="sm">
                <Chip.Label>{status}</Chip.Label>
              </Chip>
              <span className="relative z-10">
                <Tooltip>
                  <Button
                    aria-label={newNoteLabel}
                    size="sm"
                    variant="tertiary"
                    onPress={() => openPatient(visit, "newVisit")}
                  >
                    <FaIcon aria-hidden="true" icon="calendar-plus" />
                    <span className="hidden @2xl:inline">{t("clinic.appointments.visits.newNote")}</span>
                  </Button>
                  <Tooltip.Content>{newNoteLabel}</Tooltip.Content>
                </Tooltip>
              </span>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
