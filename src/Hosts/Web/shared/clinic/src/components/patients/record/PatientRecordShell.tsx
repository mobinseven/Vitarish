"use client";

import { useEffect, useRef, useState, type ReactNode } from "react";
import { Button, Card, Tabs, Tooltip } from "@heroui/react";
import { useQueryClient } from "@tanstack/react-query";
import {
  ErrorStateSurface as ClinicErrorState,
  FaIcon,
  LoadingStateSurface as ClinicLoadingState,
  PageBackNavigation,
} from "@vitarish/ui";
import { usePatientDetail } from "../../../hooks/patients/usePatientDetail";
import { useEnsurePatientVisitsProcedure } from "../../../hooks/patients/procedures/useEnsurePatientVisitsProcedure";
import { useLocalizedText, type MessageKey } from "../../../localization";
import type { NavigationIntent } from "../../../navigation";
import { pageAncestors, usePageBack } from "../../navigation/usePageBack";
import { PatientOverviewHeader } from "../overview/PatientOverviewHeader";
import { PatientVisitEventDialog } from "../procedures/PatientVisitEventDialog";
import { PatientPrescriptionSubmissionSheet } from "../prescriptions/PatientPrescriptionSubmissionSheet";
import { patientPrescriptionHistoryQueryKey } from "../prescriptions/PatientPrescriptionDashboard";
import { PatientRecordProvider } from "./PatientRecordContext";
import { patientRecordTabs, type PatientRecordTab } from "./patientRecordTabs";

/** Quick actions another page can ask the patient record to open on arrival. */
export const patientQuickActions = ["prescription", "newVisit"] as const;

export type PatientQuickAction = (typeof patientQuickActions)[number];

const tabLabelKeys = {
  overview: "clinic.patient.tabs.overview",
  visits: "clinic.procedures.visits.title",
  surgeries: "clinic.surgeries.title",
  procedures: "clinic.procedures.title",
  prescriptions: "clinic.prescriptionFeed.title",
} as const satisfies Record<PatientRecordTab, MessageKey>;

interface PatientQuickActionButton {
  id: string;
  labelKey: MessageKey;
  icon: string;
  variant: "outline" | "primary";
  onPress: () => void;
  isDisabled?: boolean;
}

export interface PatientRecordShellProps {
  patientId: string;
  activeTab: PatientRecordTab;
  onNavigate: (intent: NavigationIntent) => void;
  /** Opens this quick action once the patient is ready. */
  initialAction?: PatientQuickAction;
  /** Called once the initial action has opened, so the caller can forget it. */
  onInitialActionHandled?: () => void;
  /** The active tab's content. */
  children: ReactNode;
}

/** The patient's identity, quick actions and tab strip; the active tab's content goes inside. */
export function PatientRecordShell({
  patientId,
  activeTab,
  onNavigate,
  initialAction,
  onInitialActionHandled,
  children,
}: PatientRecordShellProps) {
  const { t } = useLocalizedText();
  const back = usePageBack([pageAncestors.patients], onNavigate);
  const [isPrescriptionOpen, setIsPrescriptionOpen] = useState(false);
  const [isNewVisitOpen, setIsNewVisitOpen] = useState(false);
  const queryClient = useQueryClient();
  const { data: patient, isLoading, error } = usePatientDetail(patientId);
  const { visitsProcedure } = useEnsurePatientVisitsProcedure(patientId, !!patient);
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
        <Button variant="ghost" onPress={() => onNavigate({ kind: "patientIndex" })}>
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
    <PatientRecordProvider
      value={{
        patientId,
        patient,
        onNavigate,
        visitsProcedureId: visitsProcedure?.id,
        openNewVisit,
        openPrescription: () => setIsPrescriptionOpen(true),
      }}
    >
      <section
        className="mx-auto flex w-full max-w-6xl flex-col gap-4"
        aria-label={t("clinic.patient.overview.title")}
      >
        <PageBackNavigation {...back} />
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
        <Tabs
          variant="secondary"
          selectedKey={activeTab}
          onSelectionChange={(key) =>
            onNavigate({ kind: "patientDetail", patientId, tab: key as PatientRecordTab })
          }
        >
          <Tabs.ListContainer>
            <Tabs.List aria-label={t("clinic.patient.overview.title")}>
              {patientRecordTabs.map((tab) => (
                <Tabs.Tab key={tab} id={tab} className="min-h-12 px-3 text-base sm:px-5">
                  {t(tabLabelKeys[tab])}
                  <Tabs.Indicator />
                </Tabs.Tab>
              ))}
            </Tabs.List>
          </Tabs.ListContainer>
        </Tabs>
        {children}
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
    </PatientRecordProvider>
  );
}
