"use client";

import { useLocalizedText } from "../../../localization";

interface PatientProcedureProgressProps {
  eventCount: number;
  committedEventCount: number;
}

export function PatientProcedureProgress({
  eventCount,
  committedEventCount,
}: PatientProcedureProgressProps) {
  const { t } = useLocalizedText();
  const boundedCommitted = Math.min(committedEventCount, eventCount);
  const percentage =
    eventCount > 0 ? Math.round((boundedCommitted / eventCount) * 100) : 0;
  const label =
    eventCount > 0
      ? t("clinic.procedures.events.progress", {
          committed: boundedCommitted,
          count: eventCount,
        })
      : t("clinic.procedures.progressAtGlance.noSteps");

  return (
    <div className="flex min-w-0 flex-col gap-1.5">
      <div className="flex items-center justify-between gap-3 text-sm">
        <span className="font-medium text-foreground">{label}</span>
        {eventCount > 0 ? (
          <span className="shrink-0 text-muted">{percentage}%</span>
        ) : null}
      </div>
      <div
        role="progressbar"
        aria-label={label}
        aria-valuemin={0}
        aria-valuemax={eventCount}
        aria-valuenow={boundedCommitted}
        className="h-1.5 overflow-hidden rounded-full bg-separator"
      >
        <div
          className="h-full rounded-full bg-success transition-[width] duration-300 motion-reduce:transition-none"
          style={{ width: `${percentage}%` }}
        />
      </div>
    </div>
  );
}
