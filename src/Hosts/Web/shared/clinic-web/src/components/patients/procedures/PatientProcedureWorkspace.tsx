"use client";

import { useState } from "react";
import { Button, Card } from "@heroui/react";
import { ErrorStateSurface, LoadingStateSurface } from "@vitarish/web-ui";
import { usePatientProcedureDetail } from "../../../hooks/patients/procedures/usePatientProcedureDetail";
import { usePatientProcedureEvents } from "../../../hooks/patients/procedures/usePatientProcedureEvents";
import { usePatientDetail } from "../../../hooks/patients/usePatientDetail";
import { useLocalizedText } from "../../../localization";
import type { NavigationIntent } from "../../../navigation";
import { PatientOverviewHeader } from "../overview/PatientOverviewHeader";
import { PatientProcedureEventTimeline } from "./PatientProcedureEventTimeline";
import { PatientProcedureHeader } from "./PatientProcedureHeader";

interface PatientProcedureWorkspaceProps {
  patientId: string;
  procedureId: string;
  onNavigate: (intent: NavigationIntent) => void;
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
  const patientQuery = usePatientDetail(patientId);

  if (procedureQuery.isLoading || eventsQuery.isLoading || patientQuery.isLoading) {
    return <LoadingStateSurface label={t("common.states.loading")} />;
  }

  if (procedureQuery.error || !procedureQuery.data) {
    return <ErrorStateSurface title={t("clinic.procedures.notFound")} />;
  }

  return (
    <section className="mx-auto flex w-full max-w-7xl flex-col gap-8">
      {patientQuery.data ? (
        <Card className="overflow-hidden" variant="transparent">
          <PatientOverviewHeader patient={patientQuery.data} />
        </Card>
      ) : null}
      <Card className="m-0">
        <Card.Content>
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
              selectedEventId={selectedEventId}
              onSelectEvent={setSelectedEventId}
              onNavigate={onNavigate}
            />
          )}
        </Card.Content>
      </Card>
    </section>
  );
}
