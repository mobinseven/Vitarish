"use client";

import { Button, Tooltip } from "@heroui/react";
import {
  EmptyStateSurface as ClinicEmptyState,
  ErrorStateSurface as ClinicErrorState,
  FaIcon,
  LoadingStateSurface as ClinicLoadingState,
} from "@vitarish/ui";
import { formatDateTime, useLocalizedText } from "../../localization";
import type { NavigationIntent } from "../../navigation";
import { useCanPlanSurgeries } from "../../hooks/surgeries/useCanPlanSurgeries";
import { usePatientSurgeries } from "../../hooks/surgeries/usePatientSurgeries";
import {
  surgeryStatuses,
  type SurgeryStatus,
} from "../../models/surgeries/surgeryModels";
import { SurgeryStatusChip } from "./SurgeryStatusChip";

const statusIcons: Record<SurgeryStatus, string> = {
  [surgeryStatuses.planned]: "calendar-clock",
  [surgeryStatuses.completed]: "circle-check",
  [surgeryStatuses.cancelled]: "calendar-xmark",
};

interface PatientSurgeriesPanelProps {
  patientId: string;
  onNavigate: (intent: NavigationIntent) => void;
}

/** The surgeries planned for one patient, newest first. Content only; the overview page wraps it in a widget. */
export function PatientSurgeriesPanel({
  patientId,
  onNavigate,
}: PatientSurgeriesPanelProps) {
  const { t, locale } = useLocalizedText();
  const canPlan = useCanPlanSurgeries();
  const { data: surgeries, isLoading, error, refetch } = usePatientSurgeries(patientId);

  return (
    <div className="flex flex-col gap-5">
      {canPlan ? (
        <div>
          <Tooltip>
            <Button
              size="sm"
              variant="primary"
              onPress={() => onNavigate({ kind: "surgeryNew", patientId })}
            >
              <FaIcon icon="plus" aria-hidden="true" />
              {t("clinic.surgeries.actions.plan")}
            </Button>
            <Tooltip.Content>{t("clinic.surgeries.actions.plan")}</Tooltip.Content>
          </Tooltip>
        </div>
      ) : null}

      {isLoading ? (
        <ClinicLoadingState label={t("common.states.loading")} />
      ) : error ? (
        <ClinicErrorState title={t("clinic.surgeries.errors.loadFailed")}>
          <Tooltip>
            <Button variant="outline" onPress={() => void refetch()}>
              <FaIcon icon="rotate-right" aria-hidden="true" />
              {t("clinic.surgeries.actions.retry")}
            </Button>
            <Tooltip.Content>{t("clinic.surgeries.actions.retry")}</Tooltip.Content>
          </Tooltip>
        </ClinicErrorState>
      ) : surgeries?.length ? (
        <ul className="flex flex-col gap-2">
          {surgeries.map((surgery) => {
            const when = formatDateTime(surgery.scheduledAt, locale);

            return (
              <li key={surgery.id}>
                <button
                  type="button"
                  aria-label={`${surgery.name}, ${when}`}
                  className="flex w-full items-center gap-3 rounded-2xl bg-surface-secondary p-3 text-start transition-colors hover:bg-default"
                  onClick={() =>
                    onNavigate({ kind: "surgeryDetail", surgeryId: surgery.id })
                  }
                >
                  <span
                    aria-hidden="true"
                    className="flex size-10 shrink-0 items-center justify-center rounded-full bg-surface text-muted"
                  >
                    <FaIcon icon={statusIcons[surgery.status]} aria-hidden="true" />
                  </span>
                  <span className="flex min-w-0 flex-1 flex-col gap-0.5">
                    <span className="truncate text-sm font-medium text-foreground">
                      {surgery.name}
                    </span>
                    <span className="truncate text-sm text-muted">
                      {when}
                      {surgery.location ? ` · ${surgery.location}` : ""}
                    </span>
                  </span>
                  <SurgeryStatusChip status={surgery.status} />
                </button>
              </li>
            );
          })}
        </ul>
      ) : (
        <ClinicEmptyState title={t("clinic.surgeries.patientPanel.empty")} />
      )}
    </div>
  );
}
