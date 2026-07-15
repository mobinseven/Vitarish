"use client";

import { Breadcrumbs, Button } from "@heroui/react";
import { ErrorStateSurface, LoadingStateSurface } from "@vitarish/web-ui";
import { usePatientProcedureDetail } from "../../../hooks/patients/procedures/usePatientProcedureDetail";
import { usePatientProcedureEvents } from "../../../hooks/patients/procedures/usePatientProcedureEvents";
import { usePatientDetail } from "../../../hooks/patients/usePatientDetail";
import { useLocalizedText } from "../../../localization";
import type { ProcedureEventViewer } from "../../../models/patients/procedures/PatientProcedureEventItemModels";
import type { ClinicNavigationIntent } from "../../../navigation";
import { PatientProcedureEventDetailPanel } from "./PatientProcedureEventDetailPanel";

export interface ClinicPatientProcedureEventPageProps {
  patientId: string;
  procedureId: string;
  eventId: string;
  viewer?: ProcedureEventViewer;
  onNavigate: (intent: ClinicNavigationIntent) => void;
}

export function ClinicPatientProcedureEventPage({
  eventId,
  onNavigate,
  patientId,
  procedureId,
  viewer,
}: ClinicPatientProcedureEventPageProps) {
  const { t } = useLocalizedText();
  const patientQuery = usePatientDetail(patientId);
  const procedureQuery = usePatientProcedureDetail(procedureId);
  const eventsQuery = usePatientProcedureEvents(procedureId);
  const returnToProcedure = () =>
    onNavigate({ kind: "patientProcedureDetail", patientId, procedureId });

  if (patientQuery.isLoading || procedureQuery.isLoading || eventsQuery.isLoading) {
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
        <Button size="sm" variant="secondary" onPress={returnToProcedure}>
          {t("clinic.procedures.returnToProcedure")}
        </Button>
      </ErrorStateSurface>
    );
  }

  const patientName = patientQuery.data
    ? `${patientQuery.data.firstName ?? ""} ${patientQuery.data.lastName ?? ""}`.trim()
    : "";
  const event = events[eventIndex];

  return (
    <section className="mx-auto flex h-full min-h-0 w-full max-w-6xl flex-col gap-6">
      <Breadcrumbs aria-label={t("clinic.procedures.breadcrumbLabel")} className="shrink-0">
        <Breadcrumbs.Item onPress={() => onNavigate({ kind: "patientDetail", patientId })}>
          {patientName || t("clinic.patients.title")}
        </Breadcrumbs.Item>
        <Breadcrumbs.Item onPress={returnToProcedure}>
          {procedureQuery.data.name}
        </Breadcrumbs.Item>
        <Breadcrumbs.Item>{event.title}</Breadcrumbs.Item>
      </Breadcrumbs>

      <PatientProcedureEventDetailPanel
        eventId={eventId}
        isFirst={eventIndex === 0}
        isLast={eventIndex === events.length - 1}
        procedureId={procedureId}
        viewer={viewer}
        onDeleted={returnToProcedure}
      />
    </section>
  );
}
