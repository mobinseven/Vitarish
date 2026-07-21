"use client";

import { useState, type Key } from "react";
import { Button, Card, Chip, Label } from "@heroui/react";
import { ContextMenu, Timeline } from "@heroui-pro/react";
import { MenuItem } from "react-aria-components";
import { FaIcon } from "@vitarish/web-ui";
import { EmptyStateSurface as ClinicEmptyState } from "@vitarish/web-ui";
import { PatientProcedureEventForm } from "./PatientProcedureEventForm";
import { PatientProcedureEventDeleteDialog } from "./PatientProcedureEventDeleteDialog";
import { PatientProcedureEventDetailPanel } from "./PatientProcedureEventDetailPanel";
import { useLocalizedText } from "../../../localization";
import type { ClinicalEventSummary } from "../../../models/patients/procedures/PatientProcedureEventModels";
import type { NavigationIntent } from "../../../navigation";
import {
  useCommitPatientProcedureEvent,
  useMovePatientProcedureEventDown,
  useMovePatientProcedureEventUp,
  useUncommitPatientProcedureEvent,
} from "../../../hooks/patients/procedures/usePatientProcedureEventMutations";

interface PatientProcedureEventTimelineProps {
  procedureId: string;
  patientId: string;
  events: ClinicalEventSummary[];
  selectedEventId?: string;
  onSelectEvent: (eventId: string) => void;
  onNavigate: (intent: NavigationIntent) => void;
}

export function PatientProcedureEventTimeline({
  procedureId,
  patientId,
  events,
  selectedEventId,
  onNavigate,
  onSelectEvent,
}: PatientProcedureEventTimelineProps) {
  const { t } = useLocalizedText();
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [editingEventId, setEditingEventId] = useState<string | null>(null);
  const [deletingEventId, setDeletingEventId] = useState<string | null>(null);
  const commit = useCommitPatientProcedureEvent();
  const uncommit = useUncommitPatientProcedureEvent();
  const earlier = useMovePatientProcedureEventUp();
  const later = useMovePatientProcedureEventDown();

  const handleAction = (eventId: string, key: Key) => {
    const event = events.find((e) => e.id === eventId);
    if (!event) return;

    if (key === "commit") {
      event.isCommitted ? uncommit.mutate(event.id) : commit.mutate(event.id);
    } else if (key === "reorder-earlier") {
      earlier.mutate(event.id);
    } else if (key === "reorder-later") {
      later.mutate(event.id);
    } else if (key === "edit") {
      setEditingEventId(event.id);
    } else if (key === "delete") {
      setDeletingEventId(event.id);
    }
  };

  const editingEvent = editingEventId ? events.find((e) => e.id === editingEventId) : null;
  const deletingEvent = deletingEventId ? events.find((e) => e.id === deletingEventId) : null;

  return (
    <div className="w-full min-w-0">
      <Timeline
        aria-label={t("clinic.procedures.events.title")}
        density="compact"
        size="sm"
      >
        {events.map((event, index) => {
          const isSelected = event.id === selectedEventId;

          return (
            <Timeline.Item
              key={event.id}
              align="center"
              id={event.id}
              status={event.isCommitted ? "success" : "default"}
            >
              <Timeline.Marker aria-hidden="true">
                <div>
                  <i className={event.isCommitted ? "fa-solid fa-check" : ""}></i>
                </div>
              </Timeline.Marker>
              <Timeline.Content>
                <ContextMenu>
                  <ContextMenu.Trigger>
                    <Button variant="ghost"
                      className="w-full max-w-md text-center"
                      onPress={() => onSelectEvent(isSelected ? "" : event.id)}>
                      <Chip
                        className="w-full max-w-md p-2"
                        color={event.isCommitted ? "success" :isSelected ? "accent" : "default"}
                        size="sm"
                        style={{ textWrap: "wrap" }}
                        variant="soft"
                      >
                        {event.title}
                      </Chip>
                    </Button>
                  </ContextMenu.Trigger>
                  <ContextMenu.Popover>
                    <ContextMenu.Menu
                      aria-label={t("clinic.procedures.events.actionMenu.label")}
                      onAction={(key) => handleAction(event.id, key)}
                    >
                      <MenuItem
                        id="reorder-earlier"
                        isDisabled={index === 0 || earlier.isPending}
                        textValue={t("clinic.procedures.events.reorderEarlier")}
                      >
                        <FaIcon icon="arrow-up" aria-hidden="true" className="size-4" />
                        <Label>{t("clinic.procedures.events.reorderEarlier")}</Label>
                      </MenuItem>
                      <MenuItem
                        id="reorder-later"
                        isDisabled={index === events.length - 1 || later.isPending}
                        textValue={t("clinic.procedures.events.reorderLater")}
                      >
                        <FaIcon icon="arrow-down" aria-hidden="true" className="size-4" />
                        <Label>{t("clinic.procedures.events.reorderLater")}</Label>
                      </MenuItem>
                      <ContextMenu.Separator />
                      <MenuItem id="edit" textValue={t("common.actions.edit")}>
                        <FaIcon icon="pen" aria-hidden="true" className="size-4" />
                        <Label>{t("common.actions.edit")}</Label>
                      </MenuItem>
                      <ContextMenu.Separator />
                      <MenuItem
                        id="delete"
                        className="text-danger"
                        textValue={t("common.actions.delete")}
                      >
                        <FaIcon icon="trash-can" aria-hidden="true" className="size-4" />
                        <Label>{t("common.actions.delete")}</Label>
                      </MenuItem>
                    </ContextMenu.Menu>
                  </ContextMenu.Popover>
                </ContextMenu>
                {isSelected ? (
                  <Card variant="default">
                    <Card.Content>
                      <PatientProcedureEventDetailPanel
                        eventId={event.id}
                        procedureId={procedureId}
                        onDeleted={() => onSelectEvent("")}
                      />
                    </Card.Content>
                  </Card>
                ) : null}
              </Timeline.Content>
            </Timeline.Item>
          );
        })}
      </Timeline>

      {events.length === 0 ? (
        <ClinicEmptyState className="mt-3" title={t("clinic.procedures.events.empty")} />
      ) : null}

      {editingEvent ? (
        <PatientProcedureEventForm
          eventId={editingEvent.id}
          initialDescription={editingEvent.description}
          initialTitle={editingEvent.title}
          procedureId={procedureId}
          projectId={editingEvent.projectId}
          onClose={() => setEditingEventId(null)}
        />
      ) : null}

      {deletingEvent ? (
        <PatientProcedureEventDeleteDialog
          eventId={deletingEvent.id}
          eventTitle={deletingEvent.title}
          onClose={() => setDeletingEventId(null)}
          onDeleted={() => {
            setDeletingEventId(null);
            onSelectEvent("");
          }}
        />
      ) : null}
    </div>
  );
}
