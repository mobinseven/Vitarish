"use client";

import { Button, Card, Tabs, Tooltip } from "@heroui/react";
import { useState } from "react";
import { Widget } from "@heroui-pro/react";
import {
  ErrorStateSurface as ClinicErrorState,
  FaIcon,
  LoadingStateSurface as ClinicLoadingState,
} from "@vitarish/web-ui";
import { usePatientDetail } from "../../../hooks/patients/usePatientDetail";
import {
  patientVisitsProcedureName,
  useEnsurePatientVisitsProcedure,
} from "../../../hooks/patients/procedures/useEnsurePatientVisitsProcedure";
import { useLocalizedText } from "../../../localization";
import type { NavigationIntent } from "../../../navigation";
import { PatientProceduresPanel } from "../procedures/PatientProceduresPanel";
import { PatientVisitsPanel } from "../procedures/PatientVisitsPanel";
import { PatientVisitEventDialog } from "../procedures/PatientVisitEventDialog";
import { PatientReservationsPanel } from "./PatientReservationsPanel";
import { PatientOverviewHeader } from "./PatientOverviewHeader";
import { PatientProfileCard } from "./PatientProfileCard";
import { PatientPrescriptionSubmissionSheet } from "../prescriptions/PatientPrescriptionSubmissionSheet";
import { PatientPrescriptionDashboard, patientPrescriptionHistoryQueryKey } from "../prescriptions/PatientPrescriptionDashboard";
import { useQueryClient } from "@tanstack/react-query";

export interface PatientOverviewPageProps {
  patientId: string;
  onNavigate: (intent: NavigationIntent) => void;
}

export function PatientOverviewPage({
  patientId,
  onNavigate,
}: PatientOverviewPageProps) {
  const { t } = useLocalizedText();
  const [isPrescriptionOpen, setIsPrescriptionOpen] = useState(false);
  const [hasVisits, setHasVisits] = useState(false);
  const [isNewVisitOpen, setIsNewVisitOpen] = useState(false);
  const queryClient = useQueryClient();
  const { data: patient, isLoading, error } = usePatientDetail(patientId);
  const { visitsProcedure } = useEnsurePatientVisitsProcedure(
    patientId,
    !!patient,
  );
  const openNewVisit = () => {
    if (visitsProcedure) setIsNewVisitOpen(true);
  };
  if (isLoading) {
    return <ClinicLoadingState label={t("clinic.patient.detail.loading")} />;
  }

  if (error || !patient) {
    return (
      <ClinicErrorState title={t("clinic.patient.detail.notFound")}>
        <Button
          variant="ghost"
          onPress={() => onNavigate({ kind: "patientIndex" })}
        >
          {t("clinic.patients.backToPatients")}
        </Button>
      </ClinicErrorState>
    );
  }

  return (
    <section
      className="mx-auto flex w-full max-w-6xl flex-col gap-4"
      aria-label={t("clinic.patient.overview.title")}
    >
      <PatientOverviewHeader patient={patient} />
      <Card variant="transparent">
        <Card.Content>
          <div className="flex flex-wrap gap-3">
            <div className="hidden flex-wrap gap-3 md:flex">
              <Button
                variant="outline"
                onPress={() => onNavigate({ kind: "patientEdit", patientId })}
              >
                <FaIcon icon="pen" aria-hidden="true" />
                {t("clinic.patient.quickActions.edit")}
              </Button>

              <Button
                variant="primary"
                onPress={() => setIsPrescriptionOpen(true)}
              >
                <FaIcon icon="prescription-bottle-medical" aria-hidden="true" />
                {t("clinic.patient.quickActions.prescription")}
              </Button>
              <Button
                variant="primary"
                onPress={openNewVisit}
                isDisabled={!visitsProcedure}
              >
                <FaIcon icon="calendar-plus" aria-hidden="true" />
                {t("clinic.patient.quickActions.newVisit")}
              </Button>
            </div>
            <div className="flex gap-3 md:hidden">
              <Tooltip>
                <Button
                  isIconOnly
                  aria-label={t("clinic.patient.quickActions.edit")}
                  variant="outline"
                  onPress={() => onNavigate({ kind: "patientEdit", patientId })}
                >
                  <FaIcon icon="pen" aria-hidden="true" />
                </Button>
                <Tooltip.Content>
                  {t("clinic.patient.quickActions.edit")}
                </Tooltip.Content>
              </Tooltip>
              <Tooltip>
                <Button
                  isIconOnly
                  aria-label={t("clinic.patient.quickActions.newVisit")}
                  variant="primary"
                  onPress={openNewVisit}
                  isDisabled={!visitsProcedure}
                >
                  <FaIcon icon="calendar-plus" aria-hidden="true" />
                </Button>
                <Tooltip.Content>
                  {t("clinic.patient.quickActions.newVisit")}
                </Tooltip.Content>
              </Tooltip>
              <Tooltip>
                <Button
                  isIconOnly
                  aria-label={t("clinic.patient.quickActions.checkIn")}
                  variant="primary"
                  onPress={() => onNavigate({ kind: "checkIn" })}
                >
                  <FaIcon icon="calendar-check" aria-hidden="true" />
                </Button>
                <Tooltip.Content>
                  {t("clinic.patient.quickActions.checkIn")}
                </Tooltip.Content>
              </Tooltip>
            </div>
          </div>
        </Card.Content>
      </Card>
      <div className="hidden md:block"><PatientPrescriptionDashboard patientId={patientId} onNewPrescription={() => setIsPrescriptionOpen(true)} /></div>
      <div className="hidden" aria-hidden="true">
        <PatientVisitsPanel
          patientId={patientId}
          onVisibilityChange={setHasVisits}
        />
      </div>
      <div className="hidden w-full grid-cols-1 gap-3 md:grid md:grid-cols-2">
        {hasVisits ? (
          <Widget className="md:col-span-2 md:row-start-2">
            <Widget.Header>
              <Widget.Title>{t("clinic.procedures.visits.title")}</Widget.Title>
            </Widget.Header>
            <Widget.Content>
              <PatientVisitsPanel
                patientId={patientId}
                onVisibilityChange={setHasVisits}
                onNewVisit={openNewVisit}
              />
            </Widget.Content>
          </Widget>
        ) : null}
        <Widget className="md:col-span-2 md:row-start-3">
          <Widget.Header>
            <Widget.Title>{t("clinic.procedures.title")}</Widget.Title>
          </Widget.Header>
          <Widget.Content>
            <PatientProceduresPanel
              patientId={patientId}
              onNavigate={onNavigate}
              excludedProcedureName={patientVisitsProcedureName}
            />
          </Widget.Content>
        </Widget>
        <Widget>
          <Widget.Header>
            <Widget.Title>
              {t("clinic.appointments.reservations.title")}
            </Widget.Title>
          </Widget.Header>
          <Widget.Content>
            <PatientReservationsPanel
              patientId={patientId}
              onCreateReservation={() =>
                onNavigate({ kind: "ownerReservationIndex" })
              }
            />
          </Widget.Content>
        </Widget>
        <Widget>
          <Widget.Header>
            <Widget.Title>{t("clinic.patient.sections.profile")}</Widget.Title>
          </Widget.Header>
          <Widget.Content>
            <PatientProfileCard patient={patient} />
          </Widget.Content>
        </Widget>
      </div>
      <Widget className="md:hidden">
        <Widget.Content>
          <Tabs defaultSelectedKey="prescriptions" className="w-full">
            <Tabs.ListContainer className="flex justify-center border-b border-border px-1">
              <Tabs.List
                aria-label={t("clinic.patient.overview.title")}
                className="w-full justify-between sm:w-auto sm:justify-center"
              >
                <Tabs.Tab id="prescriptions" className="min-h-12 px-3 text-base sm:px-5"><FaIcon icon="prescription-bottle-medical" aria-hidden="true" className="mr-2" />{t("clinic.prescriptionFeed.title")}<Tabs.Indicator /></Tabs.Tab>
                <Tabs.Tab
                  id="procedures"
                  className="min-h-12 px-3 text-base sm:px-5"
                >
                  <FaIcon
                    icon="list-check"
                    aria-hidden="true"
                    className="mr-2"
                  />
                  {t("clinic.procedures.title")}
                  <Tabs.Indicator />
                </Tabs.Tab>
                <Tabs.Tab
                  id="reservations"
                  className="min-h-12 px-3 text-base sm:px-5"
                >
                  <FaIcon icon="calendar" aria-hidden="true" className="mr-2" />
                  {t("clinic.appointments.reservations.title")}
                  <Tabs.Indicator />
                </Tabs.Tab>
                <Tabs.Tab
                  id="profile"
                  className="min-h-12 px-3 text-base sm:px-5"
                >
                  <FaIcon icon="user" aria-hidden="true" className="mr-2" />
                  {t("clinic.patient.sections.profile")}
                  <Tabs.Indicator />
                </Tabs.Tab>
              </Tabs.List>
            </Tabs.ListContainer>
            <Tabs.Panel id="prescriptions" className="pt-4"><PatientPrescriptionDashboard patientId={patientId} onNewPrescription={() => setIsPrescriptionOpen(true)} /></Tabs.Panel>
            <Tabs.Panel id="procedures" className="pt-4">
              {hasVisits ? (
                <div className="mb-4">
                  <h2 className="mb-2 text-base font-semibold">
                    {t("clinic.procedures.visits.title")}
                  </h2>
                  <PatientVisitsPanel
                    patientId={patientId}
                    onVisibilityChange={setHasVisits}
                    onNewVisit={openNewVisit}
                  />
                </div>
              ) : null}
              <PatientProceduresPanel
                patientId={patientId}
                onNavigate={onNavigate}
                excludedProcedureName={patientVisitsProcedureName}
              />
            </Tabs.Panel>
            <Tabs.Panel id="reservations" className="pt-4">
              <PatientReservationsPanel
                patientId={patientId}
                onCreateReservation={() =>
                  onNavigate({ kind: "ownerReservationIndex" })
                }
              />
            </Tabs.Panel>
            <Tabs.Panel id="profile" className="pt-4">
              <PatientProfileCard patient={patient} />
            </Tabs.Panel>
          </Tabs>
        </Widget.Content>
      </Widget>
      <PatientPrescriptionSubmissionSheet
        patient={patient}
        isOpen={isPrescriptionOpen}
        onOpenChange={setIsPrescriptionOpen}
        onCreated={(eventId) => {
          void queryClient.invalidateQueries({ queryKey: patientPrescriptionHistoryQueryKey(patientId) });
          if (!visitsProcedure) return;
          setIsPrescriptionOpen(false);
          onNavigate({
            kind: "patientProcedureEventDetail",
            patientId,
            procedureId: visitsProcedure.id,
            eventId,
          });
        }}
      />
      {visitsProcedure ? (
        <PatientVisitEventDialog
          patientId={patientId}
          procedureId={visitsProcedure.id}
          isOpen={isNewVisitOpen}
          onOpenChange={setIsNewVisitOpen}
          onCreated={(eventId) =>
            onNavigate({
              kind: "patientProcedureEventDetail",
              patientId,
              procedureId: visitsProcedure.id,
              eventId,
            })
          }
        />
      ) : null}
    </section>
  );
}
