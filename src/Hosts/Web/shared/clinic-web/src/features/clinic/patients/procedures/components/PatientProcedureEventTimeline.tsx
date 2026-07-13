"use client";

import { useState } from "react";
import { Button, Chip } from "@heroui/react";
import { Timeline } from "@heroui-pro/react";
import { EmptyStateSurface as ClinicEmptyState } from "@vitarish/web-ui";
import {
  useMovePatientProcedureEventUp,
  useMovePatientProcedureEventDown,
  useCommitPatientProcedureEvent,
  useUncommitPatientProcedureEvent,
} from "../hooks/usePatientProcedureEventMutations";
import { PatientProcedureEventForm } from "./PatientProcedureEventForm";
import { useLocalizedText, formatDateTime } from "../../../../../localization";
import type { ClinicalEventSummary } from "../models/PatientProcedureEventModels";

interface PatientProcedureEventTimelineProps {
  procedureId: string;
  patientId: string;
  events: ClinicalEventSummary[];
  onNavigate?: (intent: import("../../../../../navigation").ClinicNavigationIntent) => void;
}

export function PatientProcedureEventTimeline({ procedureId, patientId, events }: PatientProcedureEventTimelineProps) {
  const { t, locale } = useLocalizedText();
  const [showCreateForm, setShowCreateForm] = useState(false);

  const moveUpMutation = useMovePatientProcedureEventUp();
  const moveDownMutation = useMovePatientProcedureEventDown();
  const commitMutation = useCommitPatientProcedureEvent();
  const uncommitMutation = useUncommitPatientProcedureEvent();

  return (
    <>
      <div className="flex justify-end mb-3">
        <Button size="sm" variant="primary" onPress={() => setShowCreateForm(true)}>
          {t("clinic.procedures.events.addStep")}
        </Button>
      </div>

      {events.length === 0 ? (
        <ClinicEmptyState title={t("clinic.procedures.events.empty")} />
      ) : (
        <Timeline density="compact" size="sm">
          {events.map((event, index) => (
            <Timeline.Item
              key={event.id}
              status={event.isCommitted ? "success" : "default"}
            >
              <Timeline.Marker aria-hidden="true" />
              <Timeline.Content className="gap-1.5">
                <div className="flex min-w-0 flex-col gap-1 sm:flex-row sm:items-center sm:justify-between sm:gap-4">
                  <div className="flex min-w-0 flex-wrap items-center gap-2">
                    <h3 className="text-foreground m-0 text-xs font-medium leading-5">
                      {event.title}
                    </h3>
                    <Chip size="sm" color={event.isCommitted ? "success" : "default"} variant="soft">
                      {event.isCommitted ? t("clinic.procedures.committed") : t("clinic.procedures.draft")}
                    </Chip>
                  </div>
                  <time className="text-muted shrink-0 text-xs leading-5">
                    {formatDateTime(event.createdAt, locale)}
                  </time>
                </div>
                {event.committedAt && (
                  <p className="text-muted m-0 text-xs leading-5">
                    {t("clinic.procedures.committed")} {formatDateTime(event.committedAt, locale)}
                  </p>
                )}
                <div className="flex items-center gap-1">
                  <Button
                    size="sm"
                    variant="ghost"
                    isDisabled={index === 0 || moveUpMutation.isPending}
                    onPress={() => moveUpMutation.mutate(event.id)}
                  >
                    <i aria-hidden className="fa-solid fa-chevron-up" />
                  </Button>
                  <Button
                    size="sm"
                    variant="ghost"
                    isDisabled={index === events.length - 1 || moveDownMutation.isPending}
                    onPress={() => moveDownMutation.mutate(event.id)}
                  >
                    <i aria-hidden className="fa-solid fa-chevron-down" />
                  </Button>
                  <Button
                    size="sm"
                    variant={event.isCommitted ? "secondary" : "primary"}
                    isDisabled={commitMutation.isPending || uncommitMutation.isPending}
                    onPress={() =>
                      event.isCommitted
                        ? uncommitMutation.mutate(event.id)
                        : commitMutation.mutate(event.id)
                    }
                  >
                    {event.isCommitted ? t("clinic.procedures.events.reopen") : t("clinic.procedures.events.commit")}
                  </Button>
                </div>
              </Timeline.Content>
            </Timeline.Item>
          ))}
        </Timeline>
      )}

      {showCreateForm && (
        <PatientProcedureEventForm
          procedureId={procedureId}
          projectId={patientId}
          onClose={() => setShowCreateForm(false)}
        />
      )}
    </>
  );
}
