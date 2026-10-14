"use client";

import { Button, Card, Tabs, Tooltip } from "@heroui/react";
import { useEffect, useRef, useState } from "react";
import {
  ErrorStateSurface as ClinicErrorState,
  FaIcon,
  LoadingStateSurface as ClinicLoadingState,
} from "@vitarish/ui";
import { usePatientDetail } from "../../../hooks/patients/usePatientDetail";
import {
  patientVisitsProcedureName,
  useEnsurePatientVisitsProcedure,
} from "../../../hooks/patients/procedures/useEnsurePatientVisitsProcedure";
import { useLocalizedText, type MessageKey } from "../../../localization";
import type { NavigationIntent } from "../../../navigation";
import { PatientProceduresPanel } from "../procedures/PatientProceduresPanel";
import { PatientVisitsPanel } from "../procedures/PatientVisitsPanel";
import { PatientVisitEventDialog } from "../procedures/PatientVisitEventDialog";
import { PatientReservationsPanel } from "./PatientReservationsPanel";
import { PatientOverviewHeader } from "./PatientOverviewHeader";
import { PatientProfileCard } from "./PatientProfileCard";
import { PatientSurgeriesPanel } from "../../surgeries/PatientSurgeriesPanel";
import { PatientPrescriptionSubmissionSheet } from "../prescriptions/PatientPrescriptionSubmissionSheet";
import { PatientPrescriptionDashboard, patientPrescriptionHistoryQueryKey } from "../prescriptions/PatientPrescriptionDashboard";
import { useQueryClient } from "@tanstack/react-query";

/** Quick actions another page can ask the patient page to open on arrival. */
export const patientQuickActions = ["prescription", "newVisit"] as const;

export type PatientQuickAction = (typeof patientQuickActions)[number];

interface PatientQuickActionButton {
  id: string;
  labelKey: MessageKey;
  icon: string;
  variant: "outline" | "primary";
  onPress: () => void;
  isDisabled?: boolean;
}

export interface PatientOverviewPageProps {
  patientId: string;
  onNavigate: (intent: NavigationIntent) => void;
  /** Opens this quick action once the patient is ready. */
  initialAction?: PatientQuickAction;
  /** Called once the initial action has opened, so the caller can forget it. */
  onInitialActionHandled?: () => void;
}

export function PatientOverviewPage({
  patientId,
  onNavigate,
  initialAction,
  onInitialActionHandled,
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
  const handledInitialAction = useRef(false);
  useEffect(() => {
    if (!initialAction || handledInitialAction.current || !patient) return;
    if (initialAction === "prescription") setIsPrescriptionOpen(true);
    else if (visitsProcedure) setIsNewVisitOpen(true);
    else return;
    handledInitialAction.current = true;
    onInitialActionHandled?.();
  }, [initialAction, onInitialActionHandled, patient, visitsProcedure]);
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

  const quickActions: readonly PatientQuickActionButton[] = [
    {
      id: "edit",
      labelKey: "clinic.patient.quickActions.edit",
      icon: "pen",
      variant: "outline",
      onPress: () => onNavigate({ kind: "patientEdit", patientId }),
    },
    {
      id: "prescription",
      labelKey: "clinic.patient.quickActions.prescription",
      icon: "prescription-bottle-medical",
      variant: "primary",
      onPress: () => setIsPrescriptionOpen(true),
    },
    {
      id: "newVisit",
      labelKey: "clinic.patient.quickActions.newVisit",
      icon: "calendar-plus",
      variant: "outline",
      onPress: openNewVisit,
      isDisabled: !visitsProcedure,
    },
    {
      id: "checkIn",
      labelKey: "clinic.patient.quickActions.checkIn",
      icon: "calendar-check",
      variant: "outline",
      onPress: () => onNavigate({ kind: "checkIn" }),
    },
  ];

  return (
    <section
      className="mx-auto flex w-full max-w-6xl flex-col gap-4"
      aria-label={t("clinic.patient.overview.title")}
    >
      <PatientOverviewHeader patient={patient} />
      <Card variant="transparent">
        <Card.Content>
          <div className="flex flex-wrap gap-3">
            {quickActions.map((action) => (
              <Tooltip key={action.id}>
                <Button
                  aria-label={t(action.labelKey)}
                  variant={action.variant}
                  onPress={action.onPress}
                  isDisabled={action.isDisabled}
                >
                  <FaIcon icon={action.icon} aria-hidden="true" />
                  <span className="hidden md:inline">{t(action.labelKey)}</span>
                </Button>
                <Tooltip.Content>{t(action.labelKey)}</Tooltip.Content>
              </Tooltip>
            ))}
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
          <Card className="md:col-span-2 md:row-start-2">
            <Card.Header>
              <Card.Title>{t("clinic.procedures.visits.title")}</Card.Title>
            </Card.Header>
            <Card.Content>
              <PatientVisitsPanel
                patientId={patientId}
                onVisibilityChange={setHasVisits}
                onNewVisit={openNewVisit}
              />
            </Card.Content>
          </Card>
        ) : null}
        <Card className="md:col-span-2 md:row-start-3">
          <Card.Header>
            <Card.Title>{t("clinic.procedures.title")}</Card.Title>
          </Card.Header>
          <Card.Content>
            <PatientProceduresPanel
              patientId={patientId}
              onNavigate={onNavigate}
              excludedProcedureName={patientVisitsProcedureName}
            />
          </Card.Content>
        </Card>
        <Card>
          <Card.Header>
            <Card.Title>{t("clinic.surgeries.title")}</Card.Title>
          </Card.Header>
          <Card.Content>
            <PatientSurgeriesPanel
              patientId={patientId}
              onNavigate={onNavigate}
            />
          </Card.Content>
        </Card>
        <Card>
          <Card.Header>
            <Card.Title>
              {t("clinic.appointments.reservations.title")}
            </Card.Title>
          </Card.Header>
          <Card.Content>
            <PatientReservationsPanel
              patientId={patientId}
              onCreateReservation={() =>
                onNavigate({ kind: "ownerReservationIndex" })
              }
            />
          </Card.Content>
        </Card>
        <Card className="md:col-span-2">
          <Card.Header>
            <Card.Title>{t("clinic.patient.sections.profile")}</Card.Title>
          </Card.Header>
          <Card.Content>
            <PatientProfileCard patient={patient} />
          </Card.Content>
        </Card>
      </div>
      <Card className="md:hidden">
        <Card.Content>
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
                  id="surgeries"
                  className="min-h-12 px-3 text-base sm:px-5"
                >
                  <FaIcon icon="scissors" aria-hidden="true" className="mr-2" />
                  {t("clinic.surgeries.title")}
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
            <Tabs.Panel id="surgeries" className="pt-4">
              <PatientSurgeriesPanel patientId={patientId} onNavigate={onNavigate} />
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
        </Card.Content>
      </Card>
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
