"use client";

import { useState, type ReactNode } from "react";
import { Button, Chip, Surface, Tooltip } from "@heroui/react";
import {
  DetailGrid,
  ErrorStateSurface,
  FaIcon,
  HeaderIcon,
  LoadingStateSurface,
  removeButtonClassName,
} from "@vitarish/ui";
import { ApiError } from "../../api/client/apiFetch";
import {
  formatDateTime,
  useLocalizedText,
  type Translate,
} from "../../localization";
import { useCanPlanSurgeries } from "../../hooks/surgeries/useCanPlanSurgeries";
import { useSurgery } from "../../hooks/surgeries/useSurgery";
import { useSurgeryTypes } from "../../hooks/surgeries/useSurgeryTypes";
import {
  canCancelSurgery,
  canCompleteSurgery,
  canEditSurgery,
  surgeryEnd,
  surgeryStatuses,
} from "../../models/surgeries/surgeryModels";
import type { NavigationIntent } from "../../navigation";
import { SurgeryStatusChip } from "./SurgeryStatusChip";
import {
  SurgeryStatusDialogs,
  type SurgeryStatusAction,
} from "./SurgeryStatusDialogs";

export interface SurgeryDetailPageProps {
  surgeryId: string;
  onNavigate: (intent: NavigationIntent) => void;
}

function reminderOffsetLabel(minutesBefore: number, t: Translate): string {
  if (!Number.isFinite(minutesBefore) || minutesBefore <= 0) {
    return t("clinic.surgeries.detail.reminders.atStart");
  }
  if (minutesBefore % 1440 === 0) {
    return t("clinic.surgeries.detail.reminders.daysBefore", {
      count: String(minutesBefore / 1440),
    });
  }
  if (minutesBefore % 60 === 0) {
    return t("clinic.surgeries.detail.reminders.hoursBefore", {
      count: String(minutesBefore / 60),
    });
  }
  return t("clinic.surgeries.detail.reminders.minutesBefore", {
    count: String(minutesBefore),
  });
}

function DetailSection({
  icon,
  title,
  children,
}: {
  icon: string;
  title: string;
  children: ReactNode;
}) {
  return (
    <Surface className="flex flex-col gap-6 rounded-3xl p-6">
      <div className="flex items-center gap-3">
        <HeaderIcon icon={icon} />
        <div className="min-w-0">
          <h2 className="text-lg font-semibold text-foreground">{title}</h2>
        </div>
      </div>
      {children}
    </Surface>
  );
}

/** A surgery's read-only schedule, team, tools, and reminders, with its status actions. */
export function SurgeryDetailPage({
  surgeryId,
  onNavigate,
}: SurgeryDetailPageProps) {
  const { t, locale, direction } = useLocalizedText();
  const canPlan = useCanPlanSurgeries();
  const [dialogAction, setDialogAction] = useState<SurgeryStatusAction | null>(
    null,
  );
  const surgeryQuery = useSurgery(surgeryId);
  const typesQuery = useSurgeryTypes();
  const surgery = surgeryQuery.data;

  if (surgeryQuery.isPending) {
    return <LoadingStateSurface label={t("clinic.surgeries.detail.loading")} />;
  }

  if (surgeryQuery.isError || !surgery) {
    const isNotFound =
      surgeryQuery.error instanceof ApiError &&
      surgeryQuery.error.status === 404;
    const actionLabel = isNotFound
      ? t("clinic.surgeries.detail.backToSurgeries")
      : t("clinic.surgeries.actions.retry");

    return (
      <ErrorStateSurface
        title={
          isNotFound
            ? t("clinic.surgeries.detail.notFoundTitle")
            : t("clinic.surgeries.errors.loadFailed")
        }
      >
        <Tooltip>
          <Button
            variant={isNotFound ? "tertiary" : "outline"}
            onPress={
              isNotFound
                ? () => onNavigate({ kind: "surgeryIndex" })
                : () => void surgeryQuery.refetch()
            }
          >
            <FaIcon
              aria-hidden="true"
              icon={
                isNotFound
                  ? direction === "rtl"
                    ? "arrow-right"
                    : "arrow-left"
                  : "rotate-right"
              }
            />
            {actionLabel}
          </Button>
          <Tooltip.Content>{actionLabel}</Tooltip.Content>
        </Tooltip>
      </ErrorStateSurface>
    );
  }

  const end = surgeryEnd(surgery);
  const typeName = typesQuery.types.find(
    (type) => type.id === surgery.catalogId,
  )?.name;
  const canComplete = canCompleteSurgery(surgery, new Date());
  const patientName =
    surgery.patientDisplayName?.trim() || t("clinic.patients.unnamedPatient");
  const isClosed = surgery.status !== surgeryStatuses.planned;
  const notSet = t("clinic.surgeries.detail.schedule.notSet");

  const scheduleItems = [
    {
      label: t("clinic.surgeries.fields.scheduledAt"),
      value: formatDateTime(surgery.scheduledAt, locale),
    },
    {
      label: t("clinic.surgeries.fields.duration"),
      value:
        surgery.durationMinutes !== undefined
          ? t("clinic.surgeries.durationValue", {
              minutes: String(surgery.durationMinutes),
            })
          : notSet,
    },
    {
      label: t("clinic.surgeries.detail.schedule.expectedEnd"),
      value: end ? formatDateTime(end, locale) : notSet,
    },
    {
      label: t("clinic.surgeries.fields.location"),
      value: surgery.location?.trim() || notSet,
    },
    {
      label: t("clinic.surgeries.fields.type"),
      value: typeName ?? t("clinic.surgeries.detail.schedule.unknownType"),
    },
  ];

  return (
    <div className="mx-auto flex w-full max-w-4xl flex-col gap-6">
      <header className="flex flex-col items-start gap-5 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-4">
          <HeaderIcon icon="scissors" level="page" />
          <div className="min-w-0">
            <h1 className="text-2xl font-bold text-foreground">
              {surgery.name}
            </h1>
            <div className="mt-1 flex flex-wrap items-center gap-2">
              <Tooltip>
                <Button
                  variant="ghost"
                  size="sm"
                  onPress={() =>
                    onNavigate({
                      kind: "patientDetail",
                      patientId: surgery.patientId,
                    })
                  }
                >
                  <FaIcon icon="user" aria-hidden="true" />
                  {patientName}
                </Button>
                <Tooltip.Content>
                  {t("clinic.surgeries.detail.openPatient")}
                </Tooltip.Content>
              </Tooltip>
              <SurgeryStatusChip status={surgery.status} />
            </div>
          </div>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          {canPlan && canEditSurgery(surgery) ? (
            <Tooltip>
              <Button
                variant="tertiary"
                onPress={() =>
                  onNavigate({ kind: "surgeryEdit", surgeryId: surgery.id })
                }
              >
                <FaIcon icon="pen" aria-hidden="true" />
                {t("clinic.surgeries.actions.edit")}
              </Button>
              <Tooltip.Content>
                {t("clinic.surgeries.actions.edit")}
              </Tooltip.Content>
            </Tooltip>
          ) : null}

          {canPlan && surgery.status === surgeryStatuses.planned ? (
            <Tooltip>
              <Button
                variant="primary"
                isDisabled={!canComplete}
                onPress={() => setDialogAction("complete")}
              >
                <FaIcon icon="circle-check" aria-hidden="true" />
                {t("clinic.surgeries.actions.complete")}
              </Button>
              <Tooltip.Content>
                {canComplete
                  ? t("clinic.surgeries.actions.complete")
                  : t("clinic.surgeries.detail.completeUnavailable")}
              </Tooltip.Content>
            </Tooltip>
          ) : null}

          {canPlan && canCancelSurgery(surgery) ? (
            <Tooltip>
              <Button
                className={`${removeButtonClassName} text-danger`}
                variant="ghost"
                onPress={() => setDialogAction("cancel")}
              >
                <FaIcon icon="ban" aria-hidden="true" />
                {t("clinic.surgeries.actions.cancel")}
              </Button>
              <Tooltip.Content>
                {t("clinic.surgeries.actions.cancel")}
              </Tooltip.Content>
            </Tooltip>
          ) : null}
        </div>
      </header>

      <DetailSection
        icon="calendar"
        title={t("clinic.surgeries.detail.sections.schedule")}
      >
        <DetailGrid items={scheduleItems} />
      </DetailSection>

      <DetailSection
        icon="users"
        title={t("clinic.surgeries.detail.sections.team")}
      >
        {surgery.team.length > 0 ? (
          <ul className="flex flex-col gap-3">
            {surgery.team.map((member, index) => (
              <li
                key={`${member.userId}-${index}`}
                className="flex items-center gap-3"
              >
                <span
                  aria-hidden="true"
                  className="flex size-10 shrink-0 items-center justify-center rounded-full bg-surface-secondary text-foreground"
                >
                  <FaIcon icon="user" aria-hidden="true" />
                </span>
                <div className="min-w-0">
                  <p className="truncate text-sm font-medium text-foreground">
                    {member.displayName ||
                      t("clinic.patients.unnamedPatient")}
                  </p>
                  {member.role ? (
                    <p className="truncate text-sm text-muted">
                      {member.role}
                    </p>
                  ) : null}
                </div>
              </li>
            ))}
          </ul>
        ) : (
          <p className="text-sm text-muted">
            {t("clinic.surgeries.detail.team.empty")}
          </p>
        )}
      </DetailSection>

      <DetailSection
        icon="screwdriver-wrench"
        title={t("clinic.surgeries.detail.sections.tools")}
      >
        {surgery.tools.length > 0 ? (
          <div className="flex flex-wrap gap-2">
            {surgery.tools.map((tool) => (
              <Chip key={tool} size="sm" variant="soft">
                <Chip.Label>{tool}</Chip.Label>
              </Chip>
            ))}
          </div>
        ) : (
          <p className="text-sm text-muted">
            {t("clinic.surgeries.detail.tools.empty")}
          </p>
        )}
      </DetailSection>

      <DetailSection
        icon="bell"
        title={t("clinic.surgeries.detail.sections.reminders")}
      >
        <div className="flex flex-col gap-3">
          {surgery.reminderPolicies.length > 0 ? (
            <ul className="flex flex-col gap-2">
              {surgery.reminderPolicies.map((policy, index) => (
                <li
                  key={`${policy.name}-${index}`}
                  className="text-sm text-foreground"
                >
                  <span className="font-medium">
                    {reminderOffsetLabel(policy.minutesBefore, t)}
                  </span>
                  {`: ${policy.subject.trim() || policy.name}`}
                </li>
              ))}
            </ul>
          ) : (
            <p className="text-sm text-muted">
              {t("clinic.surgeries.detail.reminders.empty")}
            </p>
          )}
          <p className="text-sm text-muted">
            {isClosed
              ? t("clinic.surgeries.detail.reminders.settled")
              : t("clinic.surgeries.detail.reminders.automaticNote")}
          </p>
        </div>
      </DetailSection>

      <SurgeryStatusDialogs
        surgery={surgery}
        action={dialogAction}
        onClose={() => setDialogAction(null)}
        onRefetch={() => void surgeryQuery.refetch()}
      />
    </div>
  );
}
