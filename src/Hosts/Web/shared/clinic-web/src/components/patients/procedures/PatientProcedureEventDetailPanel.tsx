"use client";

import { useState, type Key } from "react";
import { Button, Checkbox, Chip, Dropdown, Header, Label, Separator } from "@heroui/react";
import { useSeaEventCapabilities } from "@vitarish/sea-web";
import { DetailGrid, FaIcon, type DetailGridItem } from "@vitarish/web-ui";
import { formatDateTime, useLocalizedText } from "../../../localization";
import { PatientProcedureEventAccessPanel } from "./PatientProcedureEventAccessPanel";
import { PatientProcedureEventDeleteDialog } from "./PatientProcedureEventDeleteDialog";
import { PatientProcedureEventCommitDialog } from "./PatientProcedureEventCommitDialog";
import { PatientProcedureEventForm } from "./PatientProcedureEventForm";
import { PatientProcedureEventItemsPanel } from "./PatientProcedureEventItemsPanel";
import type { ProcedureEventViewer } from "../../../models/patients/procedures/PatientProcedureEventItemModels";
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
  viewer?: ProcedureEventViewer;
  onDeleted: () => void;
}

export function PatientProcedureEventDetailPanel({ eventId, procedureId, isFirst, isLast, viewer, onDeleted }: Props) {
  const { t, locale } = useLocalizedText();
  const query = usePatientProcedureEventDetail(eventId);
  const capabilitiesQuery = useSeaEventCapabilities(eventId);
  const commit = useCommitPatientProcedureEvent();
  const uncommit = useUncommitPatientProcedureEvent();
  const earlier = useMovePatientProcedureEventUp();
  const later = useMovePatientProcedureEventDown();
  const [editing, setEditing] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [commitConfirmationTarget, setCommitConfirmationTarget] = useState<boolean | null>(null);
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
  const handleAction = (key: Key) => {
    if (key === "commit") {
      event.isCommitted ? uncommit.mutate(event.id) : commit.mutate(event.id);
    } else if (key === "reorder-earlier") {
      earlier.mutate(event.id);
    } else if (key === "reorder-later") {
      later.mutate(event.id);
    } else if (key === "edit") {
      setEditing(true);
    } else if (key === "delete") {
      setDeleting(true);
    }
  };

  return (
    <section className="flex min-h-0 flex-1 flex-col gap-6" aria-labelledby={`clinical-step-${event.id}`}>
      <header className="flex shrink-0 flex-wrap items-start justify-between gap-3">
        <div>
          {canWrite ? (
            <Checkbox
              className="[&_[data-slot='checkbox-default-indicator--checkmark']]:size-4"
              aria-label={t("clinic.procedures.events.committedState")}
              isDisabled={commit.isPending || uncommit.isPending}
              isSelected={event.isCommitted}
              onChange={setCommitConfirmationTarget}
            >
              <Checkbox.Content className="gap-3">
                <Checkbox.Control className="size-10 rounded-full border-2 data-[selected=true]:border-success data-[selected=true]:bg-success">
                  <Checkbox.Indicator className="text-success-foreground" />
                </Checkbox.Control>
                <Label>
                  <h1 id={`clinical-step-${event.id}`} className="text-2xl font-semibold">
                    {event.title}
                  </h1>
                </Label>
              </Checkbox.Content>
            </Checkbox>
          ) :
            <Label>
              <h1 id={`clinical-step-${event.id}`} className="text-2xl font-semibold">
                {event.title}
              </h1>
            </Label>}
          {event.isCommitted
            ?
            <Chip
              className="mt-2"
              color={event.isCommitted ? "success" : "default"}
              size="sm"
              variant="soft"
            > {t("clinic.procedures.committed")}
            </Chip>
            : null}
        </div>


        {canManage ? (
          <Dropdown>
            <Dropdown.Trigger
              aria-label={t("clinic.procedures.events.actionMenu.label")}
            >
              <Button isIconOnly aria-label="Menu" variant="secondary">
                <FaIcon icon="ellipsis-vertical" aria-hidden="true" className="text-lg" />
              </Button>
            </Dropdown.Trigger>
            <Dropdown.Popover>
              <Dropdown.Menu onAction={handleAction}>
                <Dropdown.Section>
                    <>
                      <Dropdown.Item
                        id="reorder-earlier"
                        textValue={t("clinic.procedures.events.reorderEarlier")}
                        isDisabled={isFirst || earlier.isPending}
                      >
                        <Label>{t("clinic.procedures.events.reorderEarlier")}</Label>
                      </Dropdown.Item>
                      <Dropdown.Item
                        id="reorder-later"
                        textValue={t("clinic.procedures.events.reorderLater")}
                        isDisabled={isLast || later.isPending}
                      >
                        <Label>{t("clinic.procedures.events.reorderLater")}</Label>
                      </Dropdown.Item>
                    </>
                </Dropdown.Section>
                  <>
                    <Separator />
                    <Dropdown.Section>
                      <Dropdown.Item id="edit" textValue={t("common.actions.edit")}>
                        <Label>{t("common.actions.edit")}</Label>
                      </Dropdown.Item>
                    </Dropdown.Section>
                    <Separator />
                    <Dropdown.Section>
                      <Dropdown.Item
                        id="delete"
                        textValue={t("common.actions.delete")}
                        variant="danger"
                      >
                        <Label>{t("common.actions.delete")}</Label>
                      </Dropdown.Item>
                    </Dropdown.Section>
                  </>
              </Dropdown.Menu>
            </Dropdown.Popover>
          </Dropdown>
        ) : capabilitiesQuery.isLoading ? null : (
          null
        )}
      </header>

      <PatientProcedureEventItemsPanel eventId={eventId} canWrite={canWrite} viewer={viewer} />
      {canManage ? <div className="shrink-0"><PatientProcedureEventAccessPanel eventId={eventId} /></div> : null}

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

      {commitConfirmationTarget !== null ? (
        <PatientProcedureEventCommitDialog
          eventTitle={event.title}
          isCommitted={commitConfirmationTarget}
          isPending={commit.isPending || uncommit.isPending}
          onClose={() => setCommitConfirmationTarget(null)}
          onConfirm={() => {
            const mutation = commitConfirmationTarget ? commit : uncommit;
            mutation.mutate(event.id, {
              onSuccess: () => setCommitConfirmationTarget(null),
            });
          }}
        />
      ) : null}
    </section>
  );
}
