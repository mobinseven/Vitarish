"use client";

import { EmptyStateSurface as ClinicEmptyState, FaIcon } from "@vitarish/ui";
import { formatDateTime, useLocalizedText } from "../../localization";
import type { Surgery } from "../../models/surgeries/surgeryModels";
import { SurgeryStatusChip } from "./SurgeryStatusChip";

export interface SurgeryListProps {
  surgeries: Surgery[];
  onOpen: (surgery: Surgery) => void;
  /** Show the localized date with the time; otherwise show the time only. */
  showDate?: boolean;
  emptyMessage: string;
}

/** A vertical list of surgeries. Each row is one pressable button to open the surgery. */
export function SurgeryList({ surgeries, onOpen, showDate = false, emptyMessage }: SurgeryListProps) {
  const { t, locale } = useLocalizedText();

  if (surgeries.length === 0) {
    return <ClinicEmptyState title={emptyMessage} />;
  }

  return (
    <ul className="flex flex-col gap-2">
      {surgeries.map((surgery) => {
        const patientName =
          surgery.patientDisplayName?.trim() || t("clinic.patients.unnamedPatient");
        const when = showDate
          ? formatDateTime(surgery.scheduledAt, locale)
          : formatDateTime(surgery.scheduledAt, locale, { dateStyle: undefined, timeStyle: "short" });

        return (
          <li key={surgery.id}>
            <button
              type="button"
              aria-label={`${patientName}, ${surgery.name}, ${when}`}
              className="flex w-full items-center gap-3 rounded-2xl bg-surface-secondary p-3 text-start transition-colors hover:bg-default"
              onClick={() => onOpen(surgery)}
            >
              <span
                aria-hidden="true"
                className="flex size-10 shrink-0 items-center justify-center rounded-full bg-surface text-muted"
              >
                <FaIcon icon="user" size="lg" aria-hidden="true" />
              </span>
              <span className="flex min-w-0 flex-1 flex-col gap-0.5">
                <span className="truncate text-sm font-medium text-foreground">{patientName}</span>
                <span className="truncate text-sm text-muted">
                  {surgery.name}
                  {surgery.location ? ` · ${surgery.location}` : ""}
                </span>
              </span>
              <span className="flex shrink-0 items-center gap-3">
                <span className="whitespace-nowrap text-sm text-foreground">{when}</span>
                <SurgeryStatusChip status={surgery.status} />
              </span>
            </button>
          </li>
        );
      })}
    </ul>
  );
}
