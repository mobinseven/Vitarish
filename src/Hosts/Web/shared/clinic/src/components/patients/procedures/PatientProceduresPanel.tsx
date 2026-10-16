"use client";

import { useState } from "react";
import { Alert, Avatar, Button, Chip, Spinner, Tooltip } from "@heroui/react";
import { EmptyState } from "@heroui-pro/react";
import { FaIcon } from "@vitarish/ui";
import { usePatientProcedures } from "../../../hooks/patients/procedures/usePatientProcedures";
import { usePatientProcedureEvents } from "../../../hooks/patients/procedures/usePatientProcedureEvents";
import { useProcedureEventAuthorNames } from "../../../hooks/patients/procedures/useProcedureEventAuthorNames";
import { formatDate, useLocalizedText } from "../../../localization";
import type { PatientProcedureSummary } from "../../../models/patients/procedures/PatientProcedureModels";
import type { NavigationIntent } from "../../../navigation";
import { PatientProcedureTemplateSelector } from "./PatientProcedureTemplateSelector";
import { PatientProcedureProgress } from "./PatientProcedureProgress";

interface PatientProceduresPanelProps {
  patientId: string;
  onNavigate?: (intent: NavigationIntent) => void;
  excludedProcedureName?: string;
}

export function PatientProceduresPanel({
  patientId,
  onNavigate,
  excludedProcedureName,
}: PatientProceduresPanelProps) {
  const { t } = useLocalizedText();
  const {
    data: procedures,
    isLoading,
    error,
  } = usePatientProcedures(patientId);
  const [showTemplateSelector, setShowTemplateSelector] = useState(false);
  const visibleProcedures = procedures?.filter(
    (procedure) => procedure.name !== excludedProcedureName,
  );

  return (
    <>
      <div className="mb-5 flex flex-wrap items-start justify-between gap-4">
        <Tooltip>
          <Button
            size="sm"
            variant="tertiary"
            onPress={() => setShowTemplateSelector(true)}
          >
            <FaIcon icon="plus" aria-hidden="true" />
            {t("clinic.procedures.new")}
          </Button>
          <Tooltip.Content>{t("clinic.procedures.new")}</Tooltip.Content>
        </Tooltip>
      </div>

      {isLoading ? (
        <div className="flex min-h-40 items-center justify-center">
          <Spinner
            aria-label={t("common.states.loading")}
            className="self-center"
          />
        </div>
      ) : error ? (
        <Alert status="danger">
          <Alert.Indicator />
          <Alert.Content>
            <Alert.Title>{t("clinic.procedures.failedToLoad")}</Alert.Title>
          </Alert.Content>
        </Alert>
      ) : visibleProcedures?.length ? (
        <ul
          aria-label={t("clinic.procedures.title")}
          className="flex flex-col divide-y divide-separator overflow-hidden rounded-2xl border border-separator bg-surface"
        >
          {visibleProcedures.map((procedure) => (
            <PatientProcedureRow
              key={procedure.id}
              patientId={patientId}
              procedure={procedure}
              onNavigate={onNavigate}
            />
          ))}
        </ul>
      ) : (
        <EmptyState size="sm">
          <EmptyState.Header>
            <EmptyState.Media variant="icon">
              <FaIcon icon="list" />
            </EmptyState.Media>
            <EmptyState.Title>{t("clinic.procedures.empty")}</EmptyState.Title>
          </EmptyState.Header>
        </EmptyState>
      )}

      {showTemplateSelector ? (
        <PatientProcedureTemplateSelector
          patientId={patientId}
          onClose={() => setShowTemplateSelector(false)}
        />
      ) : null}
    </>
  );
}

interface PatientProcedureRowProps {
  patientId: string;
  procedure: PatientProcedureSummary;
  onNavigate?: (intent: NavigationIntent) => void;
}

/** One procedure at a glance: progress, last activity, and its newest note or file. */
function PatientProcedureRow({
  patientId,
  procedure,
  onNavigate,
}: PatientProcedureRowProps) {
  const { t, locale } = useLocalizedText();
  const events = usePatientProcedureEvents(procedure.id);
  const authorNames = useProcedureEventAuthorNames();
  const latest = (events.data ?? [])
    .flatMap((event) =>
      event.attachments.map((item) => ({ item, stepTitle: event.title })),
    )
    .sort((left, right) =>
      right.item.createdAt.localeCompare(left.item.createdAt),
    )[0];
  const openLabel = t("clinic.procedures.open");
  const lastActivity = procedure.lastActivity
    ? t("clinic.procedures.progressAtGlance.lastActivity", {
        value: formatDate(procedure.lastActivity, locale, {
          dateStyle: "medium",
        }),
      })
    : t("clinic.procedures.progressAtGlance.noActivity");

  return (
    <li className="flex flex-col gap-3 px-4 py-4 sm:px-5">
      <div className="grid items-center gap-4 md:grid-cols-[minmax(0,1fr)_minmax(12rem,16rem)_auto]">
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2">
            <h3 className="text-foreground m-0 truncate text-base font-semibold">
              {procedure.name}
            </h3>
            {procedure.newItems > 0 ? (
              <Chip size="sm" color="danger" variant="soft">
                {t("clinic.procedures.progressAtGlance.newEvidence", {
                  count: procedure.newItems,
                })}
              </Chip>
            ) : null}
          </div>
          <p className="text-muted m-0 mt-1 text-sm">{lastActivity}</p>
        </div>
        <PatientProcedureProgress
          eventCount={procedure.eventCount}
          committedEventCount={procedure.committedEventCount}
        />
        <Tooltip>
          <Button
            aria-label={`${openLabel}: ${procedure.name}`}
            size="sm"
            variant="tertiary"
            onPress={() =>
              onNavigate?.({
                kind: "patientProcedureDetail",
                patientId,
                procedureId: procedure.id,
              })
            }
          >
            <FaIcon icon="timeline" aria-hidden="true" />
            <span className="md:hidden lg:inline">{openLabel}</span>
          </Button>
          <Tooltip.Content>{openLabel}</Tooltip.Content>
        </Tooltip>
      </div>
      {latest ? (
        <div className="bg-surface-secondary flex min-w-0 items-center gap-3 rounded-2xl px-3 py-2">
          <Avatar
            color="default"
            size="sm"
            variant="soft"
            className="bg-surface-tertiary shrink-0"
          >
            <Avatar.Fallback>
              <FaIcon icon="user-doctor" aria-hidden="true" />
            </Avatar.Fallback>
          </Avatar>
          <div className="min-w-0 flex-1">
            <p dir="auto" className="text-foreground m-0 truncate text-sm">
              {latest.item.type === "note"
                ? latest.item.content
                : (latest.item.fileName ??
                  t("clinic.procedures.items.genericAttachment"))}
            </p>
            <p className="text-muted m-0 truncate text-sm">
              {[
                (latest.item.authorId
                  ? authorNames.get(latest.item.authorId)
                  : undefined) ?? t("clinic.procedures.items.careTeam"),
                latest.stepTitle,
                formatDate(latest.item.createdAt, locale, {
                  dateStyle: "medium",
                }),
              ].join(" · ")}
            </p>
          </div>
        </div>
      ) : null}
    </li>
  );
}
