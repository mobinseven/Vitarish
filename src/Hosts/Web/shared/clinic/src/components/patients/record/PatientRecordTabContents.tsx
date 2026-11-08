"use client";

import { useMemo, useState, type ReactNode } from "react";
import { Button, Card } from "@heroui/react";
import { getLocalTimeZone, today } from "@internationalized/date";
import { EmptyStateSurface, FaIcon } from "@vitarish/ui";
import { useOwnerReservations } from "../../../hooks/appointments/reservations/useOwnerReservations";
import { usePatientSurgeries } from "../../../hooks/surgeries/usePatientSurgeries";
import { formatDate, formatDateTime, useLocalizedText } from "../../../localization";
import { surgeryStatuses } from "../../../models/surgeries/surgeryModels";
import { patientVisitsProcedureName } from "../../../hooks/patients/procedures/useEnsurePatientVisitsProcedure";
import { PatientSurgeriesPanel } from "../../surgeries/PatientSurgeriesPanel";
import { PatientProfileCard } from "../overview/PatientProfileCard";
import { PatientReservationsPanel } from "../overview/PatientReservationsPanel";
import {
  PatientPrescriptionDashboard,
  usePatientPrescriptionDashboard,
} from "../prescriptions/PatientPrescriptionDashboard";
import { PatientProceduresPanel } from "../procedures/PatientProceduresPanel";
import { PatientVisitsPanel } from "../procedures/PatientVisitsPanel";
import { usePatientRecord } from "./PatientRecordContext";

interface SummaryCardProps {
  icon: string;
  title: string;
  headline: ReactNode;
  detail?: ReactNode;
  actionLabel: string;
  onAction: () => void;
}

/** One at-a-glance fact about the patient, with a way into the tab that holds the rest. */
function SummaryCard({ icon, title, headline, detail, actionLabel, onAction }: SummaryCardProps) {
  return (
    <Card>
      <Card.Header>
        <Card.Title className="flex items-center gap-2 text-sm font-medium text-muted">
          <FaIcon icon={icon} aria-hidden="true" />
          {title}
        </Card.Title>
      </Card.Header>
      <Card.Content className="flex flex-col gap-1">
        <p className="text-lg font-semibold">{headline}</p>
        {detail ? <p className="text-sm text-muted">{detail}</p> : null}
        <Button className="mt-2 self-start" size="sm" variant="ghost" onPress={onAction}>
          {actionLabel}
        </Button>
      </Card.Content>
    </Card>
  );
}

/** The state of the patient at a glance, then their profile. */
export function PatientOverviewTab() {
  const { t, locale } = useLocalizedText();
  const { patientId, patient, onNavigate } = usePatientRecord();
  const reservations = useOwnerReservations({ reservingPartyId: patientId });
  const surgeries = usePatientSurgeries(patientId);
  const prescriptions = usePatientPrescriptionDashboard(patientId);

  const nextAppointment = useMemo(() => {
    const todayString = today(getLocalTimeZone()).toString();
    return (reservations.data ?? [])
      .filter((reservation) => reservation.date >= todayString)
      .sort((a, b) => a.date.localeCompare(b.date))[0];
  }, [reservations.data]);

  const nextSurgery = useMemo(() => {
    const now = Date.now();
    return (surgeries.data ?? [])
      .filter((surgery) => surgery.status === surgeryStatuses.planned && new Date(surgery.scheduledAt).getTime() >= now)
      .sort((a, b) => new Date(a.scheduledAt).getTime() - new Date(b.scheduledAt).getTime())[0];
  }, [surgeries.data]);

  const attentionCount = Number(prescriptions.data?.attentionCount ?? 0);

  return (
    <div className="flex flex-col gap-4">
      <div className="grid gap-3 md:grid-cols-3">
        <SummaryCard
          icon="calendar"
          title={t("clinic.patient.summary.nextAppointment")}
          headline={
            nextAppointment
              ? formatDate(nextAppointment.date, locale, { dateStyle: "medium" })
              : t("clinic.patient.summary.noUpcomingAppointment")
          }
          actionLabel={t("clinic.patient.summary.view")}
          onAction={() => onNavigate({ kind: "patientDetail", patientId, tab: "visits" })}
        />
        <SummaryCard
          icon="scissors"
          title={t("clinic.patient.summary.nextSurgery")}
          headline={
            nextSurgery
              ? formatDateTime(nextSurgery.scheduledAt, locale)
              : t("clinic.patient.summary.noUpcomingSurgery")
          }
          detail={nextSurgery?.name}
          actionLabel={t("clinic.patient.summary.view")}
          onAction={() => onNavigate({ kind: "patientDetail", patientId, tab: "surgeries" })}
        />
        <SummaryCard
          icon="prescription-bottle-medical"
          title={t("clinic.patient.summary.prescriptions")}
          headline={t("clinic.patient.summary.prescriptionCount", { count: Number(prescriptions.data?.totalCount ?? 0) })}
          detail={attentionCount > 0 ? t("clinic.patient.summary.needAttention", { count: attentionCount }) : undefined}
          actionLabel={t("clinic.patient.summary.view")}
          onAction={() => onNavigate({ kind: "patientDetail", patientId, tab: "prescriptions" })}
        />
      </div>
      <Card>
        <Card.Header>
          <Card.Title>{t("clinic.patient.sections.profile")}</Card.Title>
        </Card.Header>
        <Card.Content>
          <PatientProfileCard patient={patient} />
        </Card.Content>
      </Card>
    </div>
  );
}

/** The patient's visits and their booked appointments. */
export function PatientVisitsTab() {
  const { t } = useLocalizedText();
  const { patientId, onNavigate, openNewVisit, visitsProcedureId } = usePatientRecord();
  const [hasVisits, setHasVisits] = useState<boolean | null>(null);

  return (
    <div className="flex flex-col gap-4">
      <Card>
        <Card.Header>
          <Card.Title>{t("clinic.procedures.visits.title")}</Card.Title>
        </Card.Header>
        <Card.Content>
          <PatientVisitsPanel
            patientId={patientId}
            onVisibilityChange={setHasVisits}
            onNewVisit={openNewVisit}
            onNavigate={onNavigate}
          />
          {hasVisits === false ? (
            <EmptyStateSurface title={t("clinic.procedures.visits.empty")}>
              <Button isDisabled={!visitsProcedureId} variant="primary" onPress={openNewVisit}>
                <FaIcon icon="plus" aria-hidden="true" />
                {t("clinic.procedures.visits.new")}
              </Button>
            </EmptyStateSurface>
          ) : null}
        </Card.Content>
      </Card>
      <Card>
        <Card.Header>
          <Card.Title>{t("clinic.appointments.reservations.title")}</Card.Title>
        </Card.Header>
        <Card.Content>
          <PatientReservationsPanel
            patientId={patientId}
            onCreateReservation={() => onNavigate({ kind: "ownerReservationIndex" })}
          />
        </Card.Content>
      </Card>
    </div>
  );
}

/** The surgeries planned for the patient; planning one from here fills in the patient. */
export function PatientSurgeriesTab() {
  const { t } = useLocalizedText();
  const { patientId, onNavigate } = usePatientRecord();

  return (
    <Card>
      <Card.Header>
        <Card.Title>{t("clinic.surgeries.title")}</Card.Title>
      </Card.Header>
      <Card.Content>
        <PatientSurgeriesPanel patientId={patientId} onNavigate={onNavigate} />
      </Card.Content>
    </Card>
  );
}

/** The patient's procedures, each opening its workspace. */
export function PatientProceduresTab() {
  const { t } = useLocalizedText();
  const { patientId, onNavigate } = usePatientRecord();

  return (
    <Card>
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
  );
}

/** The patient's prescription history, with a way to write a new one. */
export function PatientPrescriptionsTab() {
  const { patientId, openPrescription } = usePatientRecord();

  return <PatientPrescriptionDashboard patientId={patientId} onNewPrescription={openPrescription} />;
}
