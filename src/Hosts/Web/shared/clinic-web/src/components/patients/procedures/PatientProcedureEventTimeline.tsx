"use client";

import { useState } from "react";
import { Avatar, Button, Card, Checkbox, Chip, Tooltip } from "@heroui/react";
import { Timeline } from "@heroui-pro/react";
import { useSeaEventCapabilities } from "@vitarish/sea-web";
import { EmptyStateSurface as ClinicEmptyState } from "@vitarish/web-ui";
import { PatientProcedureEventForm } from "./PatientProcedureEventForm";
import { PatientProcedureEventCommitDialog } from "./PatientProcedureEventCommitDialog";
import { useLocalizedText, formatDateTime } from "../../../localization";
import type { ClinicalEventSummary } from "../../../models/patients/procedures/PatientProcedureEventModels";
import type { EventItemSummary } from "../../../models/patients/procedures/PatientProcedureEventItemModels";
import type { NavigationIntent } from "../../../navigation";
import { PatientProcedureEventAttachmentMiniView } from "./PatientProcedureEventAttachmentMiniView";
import {
  useCommitPatientProcedureEvent,
  useUncommitPatientProcedureEvent,
} from "../../../hooks/patients/procedures/usePatientProcedureEventMutations";

interface PatientProcedureEventTimelineProps {
  procedureId: string;
  patientId: string;
  events: ClinicalEventSummary[];
  selectedEventId?: string;
  selectedEventItems?: EventItemSummary[];
  onSelectEvent: (eventId: string) => void;
  onNavigate: (intent: NavigationIntent) => void;
}

export function PatientProcedureEventTimeline({
  procedureId,
  patientId,
  events,
  selectedEventId,
  selectedEventItems = [],
  onNavigate,
  onSelectEvent,
}: PatientProcedureEventTimelineProps) {
  const { t, locale } = useLocalizedText();
  const [showCreateForm, setShowCreateForm] = useState(false);

  return (
    <div className="w-full min-w-0">
      <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
        <Button size="sm" onPress={() => setShowCreateForm(true)}>
          {t("clinic.procedures.events.addStep")}
        </Button>
      </div>

      <Timeline
        aria-label={t("clinic.procedures.events.title")}
        density="compact"
        size="sm"
      >
        {events.map((event) => {
          const isSelected = event.id === selectedEventId;

          return (
            <Timeline.Item
              key={event.id}
              align="center"
              id={event.id}
              status={isSelected ? "current" : event.isCommitted ? "success" : "default"}
            >
              <Timeline.Marker aria-hidden="true">
                <Checkbox
                  className="[&_[data-slot='checkbox-default-indicator--checkmark']]:size-4"
                  isSelected={event.isCommitted}
                  isReadOnly
                />
              </Timeline.Marker>
              <Timeline.Content>

                {!isSelected ? (
                  <div className="flex min-w-0 flex-col gap-1 sm:flex-row sm:items-start sm:justify-between sm:gap-3">
                    <div className="flex min-w-0 flex-wrap items-center gap-2">
                      <Button
                        aria-label={event.title}
                        fullWidth
                        variant="ghost"
                        onPress={() => onSelectEvent(event.id)}
                      >
                        <span className="text-muted text-xs leading-5">{event.title}</span>
                        {event.itemCount > 0 ? (
                          <Chip
                            size="sm"
                            color={event.isCommitted ? "success" : "default"}
                            variant="soft"
                          >
                            {t("clinic.procedures.items.count", { count: event.itemCount })}
                          </Chip>
                        ) : null}
                      </Button>
                    </div>
                  </div>
                ) : null}
                {isSelected ? (
                  <Card
                    aria-label={isSelected ? t("clinic.procedures.events.focusedDetails") : undefined}
                    role={isSelected ? "region" : undefined}
                    variant={isSelected ? "secondary" : "transparent"}
                  >
                    <Card.Header>
                      {event.title}
                    </Card.Header>
                    <Card.Content className="min-w-0">
                      <div className="mt-2 flex min-w-0 flex-col gap-2">
                        <div className="flex min-w-0 flex-wrap items-center gap-2">
                          <PatientProcedureEventAttachmentMiniView items={selectedEventItems} />
                        </div>
                      </div>
                    </Card.Content>
                    <Card.Footer>
                      <div className="flex w-full flex-wrap justify-end gap-1">
                        <PatientProcedureEventAction event={event} />
                        <Button
                          size="sm"
                          variant="secondary"
                          onPress={() =>
                            onNavigate({
                              kind: "patientProcedureEventDetail",
                              patientId,
                              procedureId,
                              eventId: event.id,
                            })
                          }
                        >
                          {t("clinic.procedures.events.openDetails")}
                        </Button>
                      </div>
                    </Card.Footer>
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

      {showCreateForm ? (
        <PatientProcedureEventForm
          procedureId={procedureId}
          projectId={patientId}
          onClose={() => setShowCreateForm(false)}
        />
      ) : null}
    </div>
  );
}

function PatientProcedureEventAction({ event }: { event: ClinicalEventSummary }) {
  const { t } = useLocalizedText();
  const capabilitiesQuery = useSeaEventCapabilities(event.id);
  const commit = useCommitPatientProcedureEvent();
  const uncommit = useUncommitPatientProcedureEvent();
  const [commitConfirmationTarget, setCommitConfirmationTarget] = useState<boolean | null>(null);
  const canWrite = capabilitiesQuery.data?.canWrite ?? false;
  const isPending = commit.isPending || uncommit.isPending;
  const actionLabel = t(
    event.isCommitted
      ? "clinic.procedures.events.commitConfirmation.confirmReopen"
      : "clinic.procedures.events.commitConfirmation.confirmCommit",
  );

  return (
    <>
      <Tooltip>
        {event.isCommitted ? (
          <Button
            size="sm"
            aria-label={actionLabel}
            isDisabled={!canWrite || isPending}
            variant="danger-soft"
            onPress={() => setCommitConfirmationTarget(!event.isCommitted)}
          >
            {actionLabel}
            <i aria-hidden="true" className="fa-solid fa-arrow-rotate-right" />
          </Button>
        ) :
          <Button
            size="sm"
            aria-label={actionLabel}
            isDisabled={!canWrite || isPending}
            variant="primary"
            onPress={() => setCommitConfirmationTarget(!event.isCommitted)}
          >
            {actionLabel}
            <i aria-hidden="true" className="fa-solid fa-check" />
          </Button>
        }
        <Tooltip.Content>
          {actionLabel}
        </Tooltip.Content>
      </Tooltip>
      {commitConfirmationTarget !== null ? (
        <PatientProcedureEventCommitDialog
          eventTitle={event.title}
          isCommitted={commitConfirmationTarget}
          isPending={isPending}
          onClose={() => setCommitConfirmationTarget(null)}
          onConfirm={() => {
            const mutation = commitConfirmationTarget ? commit : uncommit;
            mutation.mutate(event.id, {
              onSuccess: () => setCommitConfirmationTarget(null),
            });
          }}
        />
      ) : null}
    </>
  );
}
