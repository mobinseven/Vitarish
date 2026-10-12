"use client";

import { useState } from "react";
import { Button, Chip, Tooltip } from "@heroui/react";
import { useSeaEventCapabilities } from "@vitarish/sea";
import { FaIcon, HeaderIcon } from "@vitarish/ui";
import { formatDateTime, useLocalizedText } from "../../../localization";
import { PatientProcedureEventCommitDialog } from "./PatientProcedureEventCommitDialog";
import { PatientProcedureEventItemsPanel } from "./PatientProcedureEventItemsPanel";
import type { ProcedureEventViewer } from "../../../models/patients/procedures/PatientProcedureEventItemModels";
import { usePatientProcedureEventDetail } from "../../../hooks/patients/procedures/usePatientProcedureEventDetail";

interface Props {
  patientId: string;
  eventId: string;
  procedureId: string;
  procedureName: string;
  viewer?: ProcedureEventViewer;
  onDeleted: () => void;
}

export function PatientProcedureEventDetailPanel({
  patientId,
  eventId,
  procedureId,
  procedureName,
  viewer,
  onDeleted,
}: Props) {
  const { t, locale } = useLocalizedText();
  const [isConfirmingCommit, setIsConfirmingCommit] = useState(false);
  const query = usePatientProcedureEventDetail(eventId);
  const capabilitiesQuery = useSeaEventCapabilities(eventId);
  if (query.isLoading) return <p>{t("common.states.loading")}</p>;
  if (query.error || !query.data)
    return <p role="alert">{t("clinic.procedures.eventNotFound")}</p>;

  const event = query.data;
  const canWrite = capabilitiesQuery.data?.canWrite ?? false;
  const headingId = `clinical-step-${event.id}`;
  const commitLabel = t(
    event.isCommitted ? "clinic.procedures.events.reopen" : "clinic.procedures.events.commit",
  );
  const dates = [
    `${t("clinic.fields.created")}: ${formatDateTime(event.createdAt, locale)}`,
    ...(event.committedAt
      ? [`${t("clinic.fields.committedAt")}: ${formatDateTime(event.committedAt, locale)}`]
      : []),
  ];
  return (
    <section
      className="flex min-h-0 flex-1 flex-col gap-6"
      aria-labelledby={headingId}
    >
      <div className="flex flex-col items-start gap-5 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex min-w-0 items-center gap-3">
          <HeaderIcon icon={event.isCommitted ? "circle-check" : "list-check"} />
          <div className="min-w-0">
            <p className="text-sm text-muted">{procedureName}</p>
            <div className="flex flex-wrap items-center gap-2">
              <h2 id={headingId} className="text-lg font-semibold text-foreground">
                {event.title}
              </h2>
              <Chip color={event.isCommitted ? "success" : "default"} size="sm" variant="soft">
                {t(
                  event.isCommitted
                    ? "clinic.procedures.events.committedState"
                    : "clinic.procedures.events.pendingState",
                )}
              </Chip>
            </div>
          </div>
        </div>
        {canWrite ? (
          <Tooltip>
            <Button
              aria-label={commitLabel}
              variant={event.isCommitted ? "secondary" : "primary"}
              onPress={() => setIsConfirmingCommit(true)}
            >
              <FaIcon icon={event.isCommitted ? "arrow-rotate-left" : "check"} aria-hidden="true" />
              <span className="hidden sm:inline">{commitLabel}</span>
            </Button>
            <Tooltip.Content>{commitLabel}</Tooltip.Content>
          </Tooltip>
        ) : null}
      </div>
      <div className="flex flex-col gap-1">
        {event.description ? (
          <p dir="auto" className="text-foreground m-0 text-base leading-relaxed">{event.description}</p>
        ) : null}
        <p className="text-muted m-0 text-sm">{dates.join(" · ")}</p>
      </div>
      <PatientProcedureEventItemsPanel
        context={{ patientId, procedureId, eventId }}
        canWrite={canWrite}
        viewer={viewer}
      />
      {isConfirmingCommit ? (
        <PatientProcedureEventCommitDialog
          eventId={event.id}
          eventTitle={event.title}
          isCommitted={event.isCommitted}
          onClose={() => setIsConfirmingCommit(false)}
        />
      ) : null}
    </section>
  );
}
