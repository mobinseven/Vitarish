"use client";

import { useState } from "react";
import { Button, Chip } from "@heroui/react";
import { useSeaEventCapabilities } from "@vitarish/sea-web";
import { DetailGrid, type DetailGridItem } from "@vitarish/web-ui";
import { formatDateTime, useLocalizedText } from "../../../localization";
import { PatientProcedureEventAccessPanel } from "./PatientProcedureEventAccessPanel";
import { PatientProcedureEventDeleteDialog } from "./PatientProcedureEventDeleteDialog";
import { PatientProcedureEventForm } from "./PatientProcedureEventForm";
import { PatientProcedureEventItemsPanel } from "./PatientProcedureEventItemsPanel";
import { usePatientProcedureEventDetail } from "../../../hooks/patients/procedures/usePatientProcedureEventDetail";
import {
  useCommitPatientProcedureEvent,
  useMovePatientProcedureEventDown,
  useMovePatientProcedureEventUp,
  useUncommitPatientProcedureEvent,
} from "../../../hooks/patients/procedures/usePatientProcedureEventMutations";

interface Props {
  eventId: string;
  procedureId: string;
  isFirst: boolean;
  isLast: boolean;
  onDeleted: () => void;
}

export function PatientProcedureEventDetailPanel({ eventId, procedureId, isFirst, isLast, onDeleted }: Props) {
  const { t, locale } = useLocalizedText();
  const query = usePatientProcedureEventDetail(eventId);
  const capabilitiesQuery = useSeaEventCapabilities(eventId);
  const commit = useCommitPatientProcedureEvent();
  const uncommit = useUncommitPatientProcedureEvent();
  const earlier = useMovePatientProcedureEventUp();
  const later = useMovePatientProcedureEventDown();
  const [editing, setEditing] = useState(false);
  const [deleting, setDeleting] = useState(false);
  if (query.isLoading) return <p>{t("common.states.loading")}</p>;
  if (query.error || !query.data) return <p role="alert">{t("clinic.procedures.eventNotFound")}</p>;

  const event = query.data;
  const canWrite = capabilitiesQuery.data?.canWrite ?? false;
  const canManage = capabilitiesQuery.data?.isElevated ?? false;
  const details: DetailGridItem[] = [
    { label: t("clinic.fields.created"), value: formatDateTime(event.createdAt, locale) },
    ...(event.committedAt ? [{ label: t("clinic.fields.committedAt"), value: formatDateTime(event.committedAt, locale) }] : []),
    ...(event.description ? [{ label: t("clinic.fields.description"), value: event.description }] : []),
  ];

  return (
    <section className="flex flex-col gap-5" aria-labelledby={`clinical-step-${event.id}`}>
      <header className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 id={`clinical-step-${event.id}`} className="text-xl font-bold">
            {event.title}
          </h2>
          <Chip
            className="mt-2"
            color={event.isCommitted ? "success" : "default"}
            size="sm"
            variant="soft"
          >
            {event.isCommitted
              ? t("clinic.procedures.committed")
              : t("clinic.procedures.draft")}
          </Chip>
        </div>

        {canWrite || canManage ? (
          <div className="flex flex-wrap gap-2">
            {canWrite ? (
              <Button
                isPending={commit.isPending || uncommit.isPending}
                size="sm"
                onPress={() =>
                  event.isCommitted ? uncommit.mutate(event.id) : commit.mutate(event.id)
                }
              >
                {event.isCommitted
                  ? t("clinic.procedures.events.reopen")
                  : t("clinic.procedures.events.commit")}
              </Button>
            ) : null}

            {canManage ? (
              <>
                <Button
                  isDisabled={isFirst || earlier.isPending}
                  size="sm"
                  variant="secondary"
                  onPress={() => earlier.mutate(event.id)}
                >
                  {t("clinic.procedures.events.reorderEarlier")}
                </Button>
                <Button
                  isDisabled={isLast || later.isPending}
                  size="sm"
                  variant="secondary"
                  onPress={() => later.mutate(event.id)}
                >
                  {t("clinic.procedures.events.reorderLater")}
                </Button>
                <Button size="sm" variant="secondary" onPress={() => setEditing(true)}>
                  {t("common.actions.edit")}
                </Button>
                <Button size="sm" variant="danger-soft" onPress={() => setDeleting(true)}>
                  {t("common.actions.delete")}
                </Button>
              </>
            ) : null}
          </div>
        ) : capabilitiesQuery.isLoading ? null : (
          <Chip size="sm" variant="soft">
            {t("clinic.procedures.readOnly")}
          </Chip>
        )}
      </header>

      <DetailGrid items={details} />
      <PatientProcedureEventItemsPanel eventId={eventId} canWrite={canWrite} />
      {canManage ? <PatientProcedureEventAccessPanel eventId={eventId} /> : null}

      {editing ? (
        <PatientProcedureEventForm
          eventId={event.id}
          initialDescription={event.description}
          initialTitle={event.title}
          procedureId={procedureId}
          projectId={event.projectId}
          onClose={() => setEditing(false)}
        />
      ) : null}

      {deleting ? (
        <PatientProcedureEventDeleteDialog
          eventId={event.id}
          eventTitle={event.title}
          onClose={() => setDeleting(false)}
          onDeleted={onDeleted}
        />
      ) : null}
    </section>
  );
}
