"use client";

import { Alert, Button, Chip, Spinner } from "@heroui/react";
import { Stepper } from "@heroui-pro/react";
import { FaIcon } from "@vitarish/web-ui";
import { usePatientProcedureEvents } from "../../../hooks/patients/procedures/usePatientProcedureEvents";
import { formatDate, useLocalizedText } from "../../../localization";
import type { NavigationIntent } from "../../../navigation";
import type { PatientProcedureSummary } from "../../../models/patients/procedures/PatientProcedureModels";

interface PatientProcedureFocusedLaneProps {
  patientId: string;
  procedure: PatientProcedureSummary;
  onNavigate?: (intent: NavigationIntent) => void;
}

export function PatientProcedureFocusedLane({
  patientId,
  procedure,
  onNavigate,
}: PatientProcedureFocusedLaneProps) {
  const { t, locale } = useLocalizedText();
  const eventsQuery = usePatientProcedureEvents(procedure.id);
  const events = eventsQuery.data ?? [];
  const focusEventIndex = Math.max(
    events.findLastIndex((event) => event.isCommitted),
    0,
  );
  const visibleEvents = events.slice(
    Math.max(focusEventIndex - 1, 0),
    Math.min(focusEventIndex === 0 ? 3 : focusEventIndex + 2, events.length),
  );
  const currentStep =
    visibleEvents.findIndex((event) => !event.isCommitted) === -1
      ? visibleEvents.length
      : visibleEvents.findIndex((event) => !event.isCommitted);
  const recentItems = events
    .flatMap((event) =>
      event.attachments.map((item) => ({
        ...item,
        eventTitle: event.title,
      })),
    )
    .sort(
      (left, right) =>
        new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime(),
    )
    .slice(0, 3);
  return (
    <div className="border-t border-separator px-4 py-5 sm:px-5">
      <div className="mb-4 flex items-center justify-between gap-3">
        <h3 className="text-base font-semibold">
          {t("clinic.procedures.progressAtGlance.steps")}
        </h3>
        {events.length > visibleEvents.length ? (
          <span className="text-sm text-muted">
            {t("clinic.procedures.progressAtGlance.moreSteps", {
              count: events.length - visibleEvents.length,
            })}
          </span>
        ) : null}
      </div>

      {eventsQuery.isLoading ? (
        <div className="flex min-h-24 items-center justify-center">
          <Spinner
            size="sm"
            aria-label={t("clinic.procedures.progressAtGlance.loadingSteps")}
          />
        </div>
      ) : eventsQuery.error ? (
        <Alert status="danger">
          <Alert.Indicator />
          <Alert.Content>
            <Alert.Title>
              {t("clinic.procedures.progressAtGlance.stepsLoadFailed")}
            </Alert.Title>
          </Alert.Content>
        </Alert>
      ) : visibleEvents.length ? (
        <Stepper
          aria-label={t("clinic.procedures.events.title")}
          currentStep={currentStep}
          orientation="vertical"
          size="sm"
        >
          {visibleEvents.map((event) => (
            <Stepper.Step
              key={event.id}
            >
              <Stepper.Indicator />
              <Stepper.Content>
                <Stepper.Title>{event.title}</Stepper.Title>
                <Stepper.Description>
                  {event.isCommitted
                    ? t("clinic.procedures.progressAtGlance.completed")
                    : t("clinic.procedures.progressAtGlance.next")}
                </Stepper.Description>
              </Stepper.Content>
              <Stepper.Separator />
            </Stepper.Step>
          ))}
        </Stepper>
      ) : (
        <p className="text-sm text-muted">
          {t("clinic.procedures.progressAtGlance.noSteps")}
        </p>
      )}

      {recentItems.length ? (
        <div className="mt-6">
          <h3 className="mb-3 text-base font-semibold">
            {t("clinic.procedures.progressAtGlance.recentUpdates")}
          </h3>
          <ul className="divide-y divide-separator">
            {recentItems.map((item) => (
              <li
                key={item.id}
                className="flex flex-col gap-1 py-3 first:pt-0 sm:flex-row sm:items-start sm:justify-between sm:gap-4"
              >
                <div className="min-w-0">
                  <p className="truncate text-sm font-medium">
                    {item.fileName ||
                      item.content ||
                      t(`clinic.procedures.items.${item.type}`)}
                  </p>
                  <p className="mt-0.5 text-xs text-muted">
                    {item.eventTitle} ·{" "}
                    {t(`clinic.procedures.items.${item.type}`)}
                  </p>
                </div>
                <p className="shrink-0 text-xs text-muted">
                  {item.authorName ?? t("clinic.procedures.items.careTeam")} ·{" "}
                  {formatDate(item.createdAt, locale, { dateStyle: "medium" })}
                </p>
              </li>
            ))}
          </ul>
        </div>
      ) : null}

      <div className="mt-5 flex flex-wrap items-center gap-2">
        <Button
          size="sm"
          variant="primary"
          onPress={() =>
            onNavigate?.({
              kind: "patientProcedureDetail",
              patientId,
              procedureId: procedure.id,
            })
          }
        >
          <FaIcon icon="timeline" aria-hidden="true" />
          {t("clinic.procedures.progressAtGlance.openTimeline")}
        </Button>

        <Button
          size="sm"
          variant="ghost"
          onPress={() => onNavigate?.({ kind: "reminders" })}
        >
          <FaIcon icon="bell" aria-hidden="true" />
          {t("clinic.procedures.progressAtGlance.manageReminders")}
        </Button>
      </div>
    </div>
  );
}
