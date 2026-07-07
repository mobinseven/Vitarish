"use client";

import { useCallback, useState } from "react";
import { useRouter } from "next/navigation";
import { Button, Chip } from "@heroui/react";
import { ListView } from "@heroui-pro/react";
import { Routes } from "@/app/routes";
import { ClinicEmptyState } from "@/features/clinic/components/ClinicStateSurface";
import {
  useMovePatientProcedureEventUp,
  useMovePatientProcedureEventDown,
  useCommitPatientProcedureEvent,
  useUncommitPatientProcedureEvent,
} from "../hooks/usePatientProcedureEventMutations";
import { PatientProcedureEventForm } from "./PatientProcedureEventForm";
import { useLocalizedText, formatDateTime } from "@/localization";
import type { ClinicalEventSummary } from "../models/PatientProcedureEventModels";

interface PatientProcedureEventTimelineProps {
  procedureId: string;
  patientId: string;
  events: ClinicalEventSummary[];
}

export function PatientProcedureEventTimeline({
  procedureId,
  patientId,
  events,
}: PatientProcedureEventTimelineProps) {
  const router = useRouter();
  const { t, locale } = useLocalizedText();
  const [showCreateForm, setShowCreateForm] = useState(false);

  const moveUpMutation = useMovePatientProcedureEventUp();
  const moveDownMutation = useMovePatientProcedureEventDown();
  const commitMutation = useCommitPatientProcedureEvent();
  const uncommitMutation = useUncommitPatientProcedureEvent();

  const handleSelect = useCallback(
    (eventId: string) =>
      router.push(Routes.Clinic.Patients.ProcedureEvent(patientId, procedureId, eventId)),
    [patientId, procedureId, router]
  );

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
        <ListView
          aria-label={t("clinic.procedures.events.title")}
          onAction={(key) => handleSelect(String(key))}
          variant="secondary"
        >
          {events.map((event, index) => (
            <ListView.Item key={event.id} id={event.id} textValue={event.title}>
              <ListView.ItemContent>
                <div className="flex flex-col gap-1">
                  <div className="flex items-center gap-2">
                    <ListView.Title>{event.title}</ListView.Title>
                    <Chip size="sm" color={event.isCommitted ? "success" : "default"} variant="soft">
                      {event.isCommitted ? t("clinic.procedures.committed") : t("clinic.procedures.draft")}
                    </Chip>
                  </div>
                  <ListView.Description>
                    {formatDateTime(event.createdAt, locale)}
                    {event.committedAt && ` — ${t("clinic.procedures.committed")} ${formatDateTime(event.committedAt, locale)}`}
                  </ListView.Description>
                </div>
              </ListView.ItemContent>
              <ListView.ItemAction>
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
              </ListView.ItemAction>
            </ListView.Item>
          ))}
        </ListView>
      )}

      {showCreateForm && (
        <PatientProcedureEventForm
          procedureId={procedureId}
          projectId={events[0]?.projectId ?? ""}
          onClose={() => setShowCreateForm(false)}
        />
      )}
    </>
  );
}
