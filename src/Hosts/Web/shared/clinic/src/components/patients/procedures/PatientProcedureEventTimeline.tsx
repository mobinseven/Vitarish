"use client";

import { useState, type Key } from "react";
import {
  Button,
  ButtonGroup,
  Chip,
  Disclosure,
  Label,
  Tooltip,
} from "@heroui/react";
import {
  ContextMenu,
  PressableFeedback,
  Sheet,
  Timeline,
} from "@heroui-pro/react";
import { MenuItem } from "react-aria-components";
import { useSeaEventCapabilities } from "@vitarish/sea";
import {
  EmptyStateSurface as ClinicEmptyState,
  FaIcon,
} from "@vitarish/ui";
import { PatientProcedureEventCommitDialog } from "./PatientProcedureEventCommitDialog";
import { PatientProcedureEventDeleteDialog } from "./PatientProcedureEventDeleteDialog";
import { PatientProcedureEventForm } from "./PatientProcedureEventForm";
import { PatientProcedureEventAttachmentMiniView } from "./PatientProcedureEventAttachmentMiniView";
import { PatientProcedureEventItemsPanel } from "./PatientProcedureEventItemsPanel";
import { useLocalizedText } from "../../../localization";
import type { ClinicalEventSummary } from "../../../models/patients/procedures/PatientProcedureEventModels";
import type { ProcedureEventViewer } from "../../../models/patients/procedures/PatientProcedureEventItemModels";
import {
  useMovePatientProcedureEventDown,
  useMovePatientProcedureEventUp,
} from "../../../hooks/patients/procedures/usePatientProcedureEventMutations";

interface PatientProcedureEventTimelineProps {
  patientId?: string;
  procedureId: string;
  events: ClinicalEventSummary[];
  viewer?: ProcedureEventViewer;
}

function EventAttachmentsDrawer({
  patientId,
  procedureId,
  eventId,
  viewer,
  onClose,
}: {
  patientId: string;
  procedureId: string;
  eventId?: string;
  viewer?: ProcedureEventViewer;
  onClose: () => void;
}) {
  const { t } = useLocalizedText();
  const capabilitiesQuery = useSeaEventCapabilities(eventId ?? "");

  return (
    <Sheet
      isOpen={Boolean(eventId)}
      placement="bottom"
      onOpenChange={(isOpen) => !isOpen && onClose()}
    >
      <Sheet.Backdrop>
        <Sheet.Content className="mx-auto max-h-[95vh] max-w-[420px]">
          <Sheet.Dialog className="flex h-[75vh] flex-col">
            <Sheet.Handle />
            <Sheet.Header>
              <Sheet.Heading>
                {t("clinic.procedures.items.attachmentsPreview")}
              </Sheet.Heading>
              <Sheet.CloseTrigger
                aria-label={t("clinic.procedures.items.closePreview")}
              >
                <FaIcon icon="xmark" aria-hidden="true" />
              </Sheet.CloseTrigger>
            </Sheet.Header>
            <Sheet.Body className="flex min-h-0 flex-1 flex-col">
              <PatientProcedureEventItemsPanel
                canWrite={capabilitiesQuery.data?.canWrite ?? false}
                context={{
                  patientId,
                  procedureId,
                  eventId: eventId ?? "",
                }}
                viewer={viewer}
              />
            </Sheet.Body>
          </Sheet.Dialog>
        </Sheet.Content>
      </Sheet.Backdrop>
    </Sheet>
  );
}

export function PatientProcedureEventTimeline({
  patientId = "",
  procedureId,
  events,
  viewer,
}: PatientProcedureEventTimelineProps) {
  const { t } = useLocalizedText();
  const [editingEventId, setEditingEventId] = useState<string | null>(null);
  const [deletingEventId, setDeletingEventId] = useState<string | null>(null);
  const [attachmentsEventId, setAttachmentsEventId] = useState<string>();
  const [committingEventId, setCommittingEventId] = useState<string | null>(null);
  const earlier = useMovePatientProcedureEventUp();
  const later = useMovePatientProcedureEventDown();

  const handleAction = (eventId: string, key: Key) => {
    const event = events.find((candidate) => candidate.id === eventId);
    if (!event) return;

    if (key === "commit") setCommittingEventId(event.id);
    else if (key === "reorder-earlier") earlier.mutate(event.id);
    else if (key === "reorder-later") later.mutate(event.id);
    else if (key === "edit") setEditingEventId(event.id);
    else if (key === "delete") setDeletingEventId(event.id);
  };

  const editingEvent = editingEventId
    ? events.find((event) => event.id === editingEventId)
    : null;
  const deletingEvent = deletingEventId
    ? events.find((event) => event.id === deletingEventId)
    : null;
  const committingEvent = committingEventId
    ? events.find((event) => event.id === committingEventId)
    : null;

  return (
    <div className="w-full min-w-0">
      <Timeline
        aria-label={t("clinic.procedures.events.title")}
        density="compact"
        size="sm"
      >
        {events.map((event, index) => (
          <Timeline.Item
            key={event.id}
            align="center"
            id={event.id}
            status={event.isCommitted ? "success" : "default"}
          >
            <Timeline.Marker aria-hidden="true">
              {event.isCommitted ? <FaIcon icon="check" /> : null}
            </Timeline.Marker>
            <Timeline.Content>
              <Disclosure>
                <Disclosure.Heading>
                  <ContextMenu>
                    <ContextMenu.Trigger>
                      <Tooltip>
                        <Button
                          slot="trigger"
                          style={{ textWrap: "wrap" }}
                          variant="tertiary"
                        >
                          {event.title}
                          {event.isCommitted ? (
                            <Chip color="success" variant="soft">
                              {t("clinic.procedures.committed")}
                            </Chip>
                          ) : null}
                          <Disclosure.Indicator />
                        </Button>
                        <Tooltip.Content>{event.description}</Tooltip.Content>
                      </Tooltip>
                    </ContextMenu.Trigger>
                    <ContextMenu.Popover>
                      <ContextMenu.Menu
                        aria-label={t(
                          "clinic.procedures.events.actionMenu.label",
                        )}
                        onAction={(key) => handleAction(event.id, key)}
                      >
                        <MenuItem
                          id="reorder-earlier"
                          isDisabled={index === 0 || earlier.isPending}
                          textValue={t(
                            "clinic.procedures.events.reorderEarlier",
                          )}
                        >
                          <FaIcon
                            icon="arrow-up"
                            aria-hidden="true"
                            className="size-4"
                          />
                          <Label>
                            {t("clinic.procedures.events.reorderEarlier")}
                          </Label>
                        </MenuItem>
                        <MenuItem
                          id="reorder-later"
                          isDisabled={
                            index === events.length - 1 || later.isPending
                          }
                          textValue={t("clinic.procedures.events.reorderLater")}
                        >
                          <FaIcon
                            icon="arrow-down"
                            aria-hidden="true"
                            className="size-4"
                          />
                          <Label>
                            {t("clinic.procedures.events.reorderLater")}
                          </Label>
                        </MenuItem>
                        <ContextMenu.Separator />
                        <MenuItem
                          id="edit"
                          textValue={t("common.actions.edit")}
                        >
                          <FaIcon
                            icon="pen"
                            aria-hidden="true"
                            className="size-4"
                          />
                          <Label>{t("common.actions.edit")}</Label>
                        </MenuItem>
                        <ContextMenu.Separator />
                        <MenuItem
                          id="delete"
                          className="text-danger"
                          textValue={t("common.actions.delete")}
                        >
                          <FaIcon
                            icon="trash-can"
                            aria-hidden="true"
                            className="size-4"
                          />
                          <Label>{t("common.actions.delete")}</Label>
                        </MenuItem>
                      </ContextMenu.Menu>
                    </ContextMenu.Popover>
                  </ContextMenu>
                </Disclosure.Heading>
                <Disclosure.Content>
                  <Disclosure.Body className="shadow-panel flex flex-col rounded-3xl bg-surface p-2">
                    <div className="flex min-w-0 flex-col gap-1 py-1">
                      {event.attachments?.length ? (
                        <PatientProcedureEventAttachmentMiniView
                          items={event.attachments}
                          viewer={viewer}
                        />
                      ) : null}
                      <div className="space-y-3">
                        <ButtonGroup>
                          <Tooltip>
                            <Button
                              aria-label={t("clinic.procedures.items.title")}
                              size="sm"
                              variant="outline"
                              onPress={() => setAttachmentsEventId(event.id)}
                            >
                              <PressableFeedback.Ripple />
                              <FaIcon icon="paperclip" aria-hidden="true" />
                              {t("clinic.procedures.items.title")}
                            </Button>
                            <Tooltip.Content>
                              {t("clinic.procedures.items.title")}
                            </Tooltip.Content>
                          </Tooltip>
                          <Tooltip>
                            <Button
                              aria-label={t(
                                event.isCommitted
                                  ? "clinic.procedures.events.reopen"
                                  : "clinic.procedures.events.commit",
                              )}
                              isDisabled={committingEventId !== null}
                              size="sm"
                              variant={
                                event.isCommitted ? "secondary" : "primary"
                              }
                              onPress={() => handleAction(event.id, "commit")}
                            >
                              <ButtonGroup.Separator />
                              <PressableFeedback.Ripple />
                              <FaIcon
                                icon={
                                  event.isCommitted
                                    ? "arrow-rotate-left"
                                    : "check"
                                }
                                aria-hidden="true"
                              />
                              {t(
                                event.isCommitted
                                  ? "clinic.procedures.events.reopen"
                                  : "clinic.procedures.events.commit",
                              )}
                            </Button>
                            <Tooltip.Content>
                              {t(
                                event.isCommitted
                                  ? "clinic.procedures.events.reopen"
                                  : "clinic.procedures.events.commit",
                              )}
                            </Tooltip.Content>
                          </Tooltip>
                        </ButtonGroup>
                      </div>
                    </div>
                  </Disclosure.Body>
                </Disclosure.Content>
              </Disclosure>
            </Timeline.Content>
          </Timeline.Item>
        ))}
      </Timeline>

      {events.length === 0 ? (
        <ClinicEmptyState
          className="mt-3"
          title={t("clinic.procedures.events.empty")}
        />
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
          onDeleted={() => setDeletingEventId(null)}
        />
      ) : null}

      {committingEvent ? (
        <PatientProcedureEventCommitDialog
          eventId={committingEvent.id}
          eventTitle={committingEvent.title}
          isCommitted={committingEvent.isCommitted}
          onClose={() => setCommittingEventId(null)}
        />
      ) : null}

      <EventAttachmentsDrawer
        patientId={patientId}
        procedureId={procedureId}
        eventId={attachmentsEventId}
        viewer={viewer}
        onClose={() => setAttachmentsEventId(undefined)}
      />
    </div>
  );
}
