"use client";

import { useState } from "react";
import { Button, Chip } from "@heroui/react";
import { Timeline } from "@heroui-pro/react";
import { EmptyStateSurface as ClinicEmptyState } from "@vitarish/web-ui";
import { PatientProcedureEventForm } from "./PatientProcedureEventForm";
import { useLocalizedText, formatDateTime } from "../../../localization";
import type { ClinicalEventSummary } from "../../../models/patients/procedures/PatientProcedureEventModels";
import type { EventItemSummary } from "../../../models/patients/procedures/PatientProcedureEventItemModels";
import type { ClinicNavigationIntent } from "../../../navigation";
import { PatientProcedureEventAttachmentMiniView } from "./PatientProcedureEventAttachmentMiniView";

interface PatientProcedureEventTimelineProps {
  procedureId: string;
  patientId: string;
  events: ClinicalEventSummary[];
  selectedEventId?: string;
  selectedEventItems?: EventItemSummary[];
  onSelectEvent: (eventId: string) => void;
  onNavigate: (intent: ClinicNavigationIntent) => void;
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
        <div>
          <p className="text-muted m-0 text-xs font-medium">
            {t("clinic.procedures.events.activityLabel")}
          </p>
          <h2 className="text-foreground m-0 text-lg font-semibold">
            {t("clinic.procedures.events.title")}
          </h2>
        </div>
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
                <i
                  className={`fa-solid ${event.isCommitted ? "fa-check" : "fa-message"} size-3`}
                />
              </Timeline.Marker>
              <Timeline.Content className="min-w-0 py-1">
                <div
                  aria-label={isSelected ? t("clinic.procedures.events.focusedDetails") : undefined}
                  className={isSelected ? "bg-surface-secondary -mx-2 rounded-xl px-2 py-2" : undefined}
                  role={isSelected ? "region" : undefined}
                >
                  <Button
                    aria-label={event.title}
                    className="h-auto min-h-0 w-full justify-start rounded-none p-0 text-start"
                    variant="ghost"
                    onPress={() => onSelectEvent(event.id)}
                  >
                    <span className="flex w-full min-w-0 flex-col gap-1 sm:flex-row sm:items-start sm:gap-4">
                      <span className="min-w-0 flex-1">
                        <span className="flex min-w-0 flex-wrap items-center gap-2">
                          <span className="text-foreground text-sm font-medium leading-5">
                            {event.title}
                          </span>
                        </span>
                      </span>
                      <span className="text-muted flex shrink-0 items-center gap-2 text-xs leading-5 sm:ms-auto">
                        {event.itemCount > 0 ? (
                          <Chip size="sm" color={event.isCommitted ? "success" : "default"}>
                            {t("clinic.procedures.items.count", { count: event.itemCount })}
                          </Chip>
                        ) : null}
                      </span>
                    </span>
                  </Button>
                  {isSelected ? (
                    <div className="mt-2 flex min-w-0 flex-col gap-2">
                      <div className="flex min-w-0 flex-wrap items-center gap-2">
                        <PatientProcedureEventAttachmentMiniView items={selectedEventItems} />
                        <Button
                          className="ms-auto min-h-8 px-2"
                          size="sm"
                          variant="ghost"
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
                    </div>
                  ) : null}
                </div>
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
