"use client";

import { Button, Card, Tabs, Tooltip } from "@heroui/react";
import {
  ErrorStateSurface as ClinicErrorState,
  FaIcon,
  LoadingStateSurface as ClinicLoadingState,
} from "@vitarish/web-ui";
import { usePatientDetail } from "../../../hooks/patients/usePatientDetail";
import { useLocalizedText } from "../../../localization";
import type { NavigationIntent } from "../../../navigation";
import { PatientProceduresPanel } from "../procedures/PatientProceduresPanel";
import { PatientOverviewHeader } from "./PatientOverviewHeader";
import { PatientProfileCard } from "./PatientProfileCard";

export interface PatientOverviewPageProps {
  patientId: string;
  onNavigate: (intent: NavigationIntent) => void;
}

export function PatientOverviewPage({
  patientId,
  onNavigate,
}: PatientOverviewPageProps) {
  const { t } = useLocalizedText();
  const { data: patient, isLoading, error } = usePatientDetail(patientId);

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
                onPress={() => onNavigate({ kind: "checkIn" })}
              >
                <FaIcon icon="calendar-check" aria-hidden="true" />
                {t("clinic.patient.quickActions.checkIn")}
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
      <div className="hidden items-start gap-4 md:grid md:grid-cols-[minmax(0,2fr)_minmax(18rem,1fr)]">
        <Card>
          <Card.Content>
            <PatientProceduresPanel
              patientId={patientId}
              onNavigate={onNavigate}
            />
          </Card.Content>
        </Card>
        <PatientProfileCard patient={patient} />
      </div>
      <Card className="md:hidden">
        <Card.Content>
          <Tabs defaultSelectedKey="procedures" className="w-full">
            <Tabs.ListContainer className="flex justify-center border-b border-border px-1">
              <Tabs.List
                aria-label={t("clinic.patient.overview.title")}
                className="w-full justify-between sm:w-auto sm:justify-center"
              >
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
                  id="profile"
                  className="min-h-12 px-3 text-base sm:px-5"
                >
                  <FaIcon icon="user" aria-hidden="true" className="mr-2" />
                  {t("clinic.patient.sections.profile")}
                  <Tabs.Indicator />
                </Tabs.Tab>
              </Tabs.List>
            </Tabs.ListContainer>
            <Tabs.Panel id="procedures" className="pt-4">
              <PatientProceduresPanel
                patientId={patientId}
                onNavigate={onNavigate}
              />
            </Tabs.Panel>
            <Tabs.Panel id="profile" className="pt-4">
              <PatientProfileCard patient={patient} />
            </Tabs.Panel>
          </Tabs>
        </Card.Content>
      </Card>
    </section>
  );
}
