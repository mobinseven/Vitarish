"use client";

import { Button, Card } from "@heroui/react";
import { useSeaEventCapabilities } from "@vitarish/sea";
import { ErrorStateSurface, LoadingStateSurface } from "@vitarish/ui";
import { usePatientProcedureDetail } from "../../../hooks/patients/procedures/usePatientProcedureDetail";
import { usePatientProcedureEvents } from "../../../hooks/patients/procedures/usePatientProcedureEvents";
import { usePatientDetail } from "../../../hooks/patients/usePatientDetail";
import { useLocalizedText } from "../../../localization";
import type { ProcedureEventViewer } from "../../../models/patients/procedures/PatientProcedureEventItemModels";
import type { NavigationIntent } from "../../../navigation";
import { PatientOverviewHeader } from "../overview/PatientOverviewHeader";
import { PatientProcedureEventAccessPanel } from "./PatientProcedureEventAccessPanel";
import { PatientProcedureEventDetailPanel } from "./PatientProcedureEventDetailPanel";

export interface PatientProcedureEventPageProps {
  patientId: string;
  procedureId: string;
  eventId: string;
  viewer?: ProcedureEventViewer;
  onNavigate: (intent: NavigationIntent) => void;
}

export function PatientProcedureEventPage({
  eventId,
  onNavigate,
  patientId,
  procedureId,
  viewer,
}: PatientProcedureEventPageProps) {
  const { t } = useLocalizedText();
  const patientQuery = usePatientDetail(patientId);
  const procedureQuery = usePatientProcedureDetail(procedureId);
  const eventsQuery = usePatientProcedureEvents(procedureId);
  const capabilitiesQuery = useSeaEventCapabilities(eventId);
  const returnToProcedure = () =>
    onNavigate({ kind: "patientProcedureDetail", patientId, procedureId });

  if (
    patientQuery.isLoading ||
    procedureQuery.isLoading ||
    eventsQuery.isLoading
  ) {
    return <LoadingStateSurface label={t("common.states.loading")} />;
  }

  if (procedureQuery.error || !procedureQuery.data) {
    return <ErrorStateSurface title={t("clinic.procedures.notFound")} />;
  }

  const events = eventsQuery.data ?? [];
  const eventIndex = events.findIndex((event) => event.id === eventId);

  if (eventsQuery.error || eventIndex < 0) {
    return (
      <ErrorStateSurface title={t("clinic.procedures.eventNotFound")}>
        <Button size="sm" variant="tertiary" onPress={returnToProcedure}>
          {t("clinic.procedures.returnToProcedure")}
        </Button>
      </ErrorStateSurface>
    );
  }

  return (
    <section className="mx-auto flex h-full min-h-0 w-full max-w-6xl flex-col gap-6">
      {patientQuery.data ? (
        <>
          <Card className="overflow-hidden" variant="transparent">
            <PatientOverviewHeader patient={patientQuery.data} />
          </Card>

          <Card>
            <Card.Content>
              <PatientProcedureEventDetailPanel
                patientId={patientId}
                eventId={eventId}
                procedureId={procedureId}
                procedureName={procedureQuery.data.name}
                viewer={viewer}
                onDeleted={returnToProcedure}
              />
            </Card.Content>
          </Card>

          {capabilitiesQuery.data?.isElevated ? (
            <PatientProcedureEventAccessPanel eventId={eventId} />
          ) : null}
        </>
      ) : null}
    </section>
  );
}
