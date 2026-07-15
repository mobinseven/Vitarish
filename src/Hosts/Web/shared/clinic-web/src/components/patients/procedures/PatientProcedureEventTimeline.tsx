"use client";

import { useState } from "react";
import { Button, Chip } from "@heroui/react";
import { Timeline } from "@heroui-pro/react";
import { EmptyStateSurface as ClinicEmptyState } from "@vitarish/web-ui";
import { PatientProcedureEventForm } from "./PatientProcedureEventForm";
import { useLocalizedText, formatDateTime } from "../../../localization";
import type { ClinicalEventSummary } from "../../../models/patients/procedures/PatientProcedureEventModels";
import type { ClinicNavigationIntent } from "../../../navigation";

interface PatientProcedureEventTimelineProps {
  procedureId: string;
  patientId: string;
  events: ClinicalEventSummary[];
  selectedEventId?: string;
  onNavigate: (intent: ClinicNavigationIntent) => void;
}

export function PatientProcedureEventTimeline({ procedureId, patientId, events, selectedEventId, onNavigate }: PatientProcedureEventTimelineProps) {
  const { t, locale } = useLocalizedText();
  const [showCreateForm, setShowCreateForm] = useState(false);

  return (
    <div className="w-full min-w-0">
      <Timeline
        aria-label={t("clinic.procedures.events.title")}
        density="compact"
        size="sm"
      >
        <Timeline.Item align="center">
          <Timeline.Marker aria-hidden="true" />
          <Timeline.Content>
            <Button size="sm" onPress={() => setShowCreateForm(true)}>
              {t("clinic.procedures.events.addStep")}
            </Button>
          </Timeline.Content>
        </Timeline.Item>

        {events.map((event) => {
          const isSelected = event.id === selectedEventId;

          return (
            <Timeline.Item
              key={event.id}
              align="center"
              id={event.id}
              status={isSelected ? "current" : event.isCommitted ? "success" : "default"}
            >
              <Timeline.Marker aria-hidden="true" />
              <Timeline.Content>
                <Button
                  aria-label={event.title}
                  className="h-auto min-h-10 w-full justify-start px-2 py-2 text-start"
                  variant={isSelected ? "secondary" : "ghost"}
                  onPress={() =>
                    onNavigate({
                      kind: "patientProcedureEventDetail",
                      patientId,
                      procedureId,
                      eventId: event.id,
                    })
                  }
                >
                  <span className="flex min-w-0 w-full flex-col gap-1 sm:flex-row sm:items-center sm:justify-between sm:gap-3">
                    <span className="flex min-w-0 flex-wrap items-center gap-2">
                      <span className="text-foreground text-xs font-medium leading-5">
                        {event.title}
                      </span>
                      <Chip
                        color={event.isCommitted ? "success" : "default"}
                        size="sm"
                        variant="soft"
                      >
                        {event.isCommitted
                          ? t("clinic.procedures.committed")
                          : t("clinic.procedures.draft")}
                      </Chip>
                    </span>
                    <time
                      className="text-muted shrink-0 text-xs leading-5"
                      dateTime={event.createdAt}
                    >
                      {formatDateTime(event.createdAt, locale)}
                    </time>
                  </span>
                </Button>
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
