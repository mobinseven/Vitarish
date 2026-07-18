"use client";

import { useState } from "react";
import { Button } from "@heroui/react";
import { ErrorStateSurface, LoadingStateSurface } from "@vitarish/web-ui";
import { usePatientProcedureDetail } from "../../../hooks/patients/procedures/usePatientProcedureDetail";
import { usePatientProcedureEvents } from "../../../hooks/patients/procedures/usePatientProcedureEvents";
import { usePatientProcedureEventItems } from "../../../hooks/patients/procedures/usePatientProcedureEventItems";
import { usePatientDetail } from "../../../hooks/patients/usePatientDetail";
import { useLocalizedText } from "../../../localization";
import type { ClinicNavigationIntent } from "../../../navigation";
import { PatientProcedureEventTimeline } from "./PatientProcedureEventTimeline";
import { PatientProcedureHeader } from "./PatientProcedureHeader";

interface PatientProcedureWorkspaceProps {
  patientId: string;
  procedureId: string;
  onNavigate: (intent: ClinicNavigationIntent) => void;
}

export function PatientProcedureWorkspace({
  patientId,
  procedureId,
  onNavigate,
}: PatientProcedureWorkspaceProps) {
  const { t } = useLocalizedText();
  const [selectedEventId, setSelectedEventId] = useState<string>();
  const procedureQuery = usePatientProcedureDetail(procedureId);
  const eventsQuery = usePatientProcedureEvents(procedureId);
  const events = eventsQuery.data ?? [];
  const activeEventId = selectedEventId ?? events.at(-1)?.id;
  const selectedEventItemsQuery = usePatientProcedureEventItems(activeEventId ?? "");
  const patientQuery = usePatientDetail(patientId);

  if (procedureQuery.isLoading || eventsQuery.isLoading || patientQuery.isLoading) {
    return <LoadingStateSurface label={t("common.states.loading")} />;
  }

  if (procedureQuery.error || !procedureQuery.data) {
    return <ErrorStateSurface title={t("clinic.procedures.notFound")} />;
  }

  return (
    <section className="mx-auto flex w-full max-w-7xl flex-col gap-8">
      <PatientProcedureHeader
        patientId={patientId}
        procedure={procedureQuery.data}
        onDeleted={() => onNavigate({ kind: "patientDetail", patientId })}
      />

      {eventsQuery.error ? (
        <ErrorStateSurface title={t("clinic.procedures.failedToLoad")}>
          <Button size="sm" variant="secondary" onPress={() => void eventsQuery.refetch()}>
            {t("common.actions.retry")}
          </Button>
        </ErrorStateSurface>
      ) : (
          <PatientProcedureEventTimeline
            events={events}
            patientId={patientId}
            procedureId={procedureId}
            selectedEventId={activeEventId}
            selectedEventItems={selectedEventItemsQuery.data ?? []}
            onSelectEvent={setSelectedEventId}
            onNavigate={onNavigate}
          />
      )}
    </section>
  );
}
