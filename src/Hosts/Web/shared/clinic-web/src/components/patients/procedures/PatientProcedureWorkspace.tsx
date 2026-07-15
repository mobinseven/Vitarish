"use client";

import { useEffect, useState } from "react";
import { Breadcrumbs, Button, Card } from "@heroui/react";
import { Sheet } from "@heroui-pro/react";
import { ErrorStateSurface, LoadingStateSurface } from "@vitarish/web-ui";
import { usePatientProcedureDetail } from "../../../hooks/patients/procedures/usePatientProcedureDetail";
import { usePatientProcedureEvents } from "../../../hooks/patients/procedures/usePatientProcedureEvents";
import { usePatientDetail } from "../../../hooks/patients/usePatientDetail";
import { useLocalizedText } from "../../../localization";
import type { ClinicNavigationIntent } from "../../../navigation";
import { PatientProcedureEventDetailPanel } from "./PatientProcedureEventDetailPanel";
import { PatientProcedureEventTimeline } from "./PatientProcedureEventTimeline";
import { PatientProcedureHeader } from "./PatientProcedureHeader";

interface PatientProcedureWorkspaceProps {
  patientId: string;
  procedureId: string;
  eventId?: string;
  onNavigate: (intent: ClinicNavigationIntent) => void;
}

function useDesktopWorkspace() {
  const [isDesktop, setIsDesktop] = useState(true);

  useEffect(() => {
    const mediaQuery = window.matchMedia("(min-width: 1024px)");
    const update = () => setIsDesktop(mediaQuery.matches);

    update();
    mediaQuery.addEventListener("change", update);
    return () => mediaQuery.removeEventListener("change", update);
  }, []);

  return isDesktop;
}

export function PatientProcedureWorkspace({
  patientId,
  procedureId,
  eventId,
  onNavigate,
}: PatientProcedureWorkspaceProps) {
  const { t } = useLocalizedText();
  const isDesktop = useDesktopWorkspace();
  const procedureQuery = usePatientProcedureDetail(procedureId);
  const eventsQuery = usePatientProcedureEvents(procedureId);
  const patientQuery = usePatientDetail(patientId);

  if (procedureQuery.isLoading || eventsQuery.isLoading || patientQuery.isLoading) {
    return <LoadingStateSurface label={t("common.states.loading")} />;
  }

  if (procedureQuery.error || !procedureQuery.data) {
    return <ErrorStateSurface title={t("clinic.procedures.notFound")} />;
  }

  const events = eventsQuery.data ?? [];
  const selectedId = eventId ?? events[0]?.id;
  const selectedIndex = selectedId ? events.findIndex((item) => item.id === selectedId) : -1;
  const closeDetail = () =>
    onNavigate({ kind: "patientProcedureDetail", patientId, procedureId });
  const patientName = patientQuery.data
    ? `${patientQuery.data.firstName ?? ""} ${patientQuery.data.lastName ?? ""}`.trim()
    : "";

  const detail =
    selectedIndex >= 0 && selectedId ? (
      <PatientProcedureEventDetailPanel
        eventId={selectedId}
        isFirst={selectedIndex === 0}
        isLast={selectedIndex === events.length - 1}
        procedureId={procedureId}
        onDeleted={closeDetail}
      />
    ) : eventId ? (
      <div className="p-4" role="alert">
        <p>{t("clinic.procedures.eventNotFound")}</p>
        <Button className="mt-3" onPress={closeDetail}>
          {t("clinic.procedures.returnToProcedure")}
        </Button>
      </div>
    ) : null;

  return (
    <section className="flex flex-col gap-6">
      <Breadcrumbs aria-label={t("clinic.procedures.breadcrumbLabel")}>
        <Breadcrumbs.Item onPress={() => onNavigate({ kind: "patientDetail", patientId })}>
          {patientName || t("clinic.patients.title")}
        </Breadcrumbs.Item>
        <Breadcrumbs.Item>{procedureQuery.data.name}</Breadcrumbs.Item>
      </Breadcrumbs>

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
        <div className="grid items-start gap-6 lg:grid-cols-[minmax(18rem,0.8fr)_minmax(0,1.5fr)]">
          <section aria-labelledby="clinic-procedure-events-title">
            <h2 id="clinic-procedure-events-title" className="mb-3 text-lg font-semibold">
              {t("clinic.procedures.events.title")}
            </h2>
            <PatientProcedureEventTimeline
              events={events}
              patientId={patientId}
              procedureId={procedureId}
              selectedEventId={selectedId}
              onNavigate={onNavigate}
            />
          </section>

          {detail && isDesktop ? (
            <Card>
              <Card.Content className="p-5">{detail}</Card.Content>
            </Card>
          ) : null}
        </div>
      )}

      {eventId && !eventsQuery.error && !isDesktop ? (
        <Sheet
          isOpen
          placement="bottom"
          onOpenChange={(isOpen) => {
            if (!isOpen) closeDetail();
          }}
        >
          <Sheet.Backdrop>
            <Sheet.Content className="mx-auto max-h-[95vh] w-full max-w-3xl">
              <Sheet.Dialog aria-label={t("clinic.procedures.selectedStep")}>
                <Sheet.Handle />
                <Sheet.CloseTrigger aria-label={t("common.actions.close")} />
                <Sheet.Header>
                  <Sheet.Heading>{t("clinic.procedures.selectedStep")}</Sheet.Heading>
                </Sheet.Header>
                <Sheet.Body className="min-h-0 max-h-[80vh] overflow-y-auto">
                  {detail}
                </Sheet.Body>
              </Sheet.Dialog>
            </Sheet.Content>
          </Sheet.Backdrop>
        </Sheet>
      ) : null}
    </section>
  );
}
