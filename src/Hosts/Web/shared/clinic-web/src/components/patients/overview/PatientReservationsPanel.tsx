"use client";

import { useMemo, useState } from "react";
import { Button, Calendar } from "@heroui/react";
import { EmptyState } from "@heroui-pro/react";
import {
  createCalendar,
  getLocalTimeZone,
  parseDate,
  today,
  toCalendar,
} from "@internationalized/date";
import {
  ErrorStateSurface as ClinicErrorState,
  FaIcon,
  LoadingStateSurface as ClinicLoadingState,
} from "@vitarish/web-ui";
import { getDateCalendar, useLocalizedText } from "../../../localization";
import { useOwnerReservations } from "../../../hooks/appointments/reservations/useOwnerReservations";
import { ReservationDetailDialog } from "../../appointments/agenda/ReservationDetailDialog";

interface PatientReservationsPanelProps {
  patientId: string;
  onCreateReservation: () => void;
}

export function PatientReservationsPanel({
  patientId,
  onCreateReservation,
}: PatientReservationsPanelProps) {
  const { t, locale } = useLocalizedText();
  const {
    data: reservations,
    isLoading,
    error,
  } = useOwnerReservations({ reservingPartyId: patientId });
  const [detailReservationId, setDetailReservationId] = useState<string | null>(
    null,
  );
  const calendar = useMemo(
    () => createCalendar(getDateCalendar(locale)),
    [locale],
  );
  const todayDate = today(getLocalTimeZone());
  const todayString = todayDate.toString();
  const upcoming = useMemo(
    () =>
      (reservations ?? [])
        .filter((reservation) => reservation.date >= todayString)
        .sort((a, b) => a.date.localeCompare(b.date)),
    [reservations, todayString],
  );
  const firstDate = upcoming[0]?.date ?? todayString;
  const focusedDate = useMemo(
    () => toCalendar(parseDate(firstDate), calendar),
    [calendar, firstDate],
  );
  const dateToReservations = useMemo(
    () =>
      new Map(
        upcoming.map((reservation) => [
          toCalendar(parseDate(reservation.date), calendar).toString(),
          reservation,
        ]),
      ),
    [calendar, upcoming],
  );

  if (isLoading)
    return <ClinicLoadingState label={t("common.states.loading")} />;
  if (error)
    return (
      <ClinicErrorState
        title={t("clinic.appointments.reservations.failedToLoad")}
      />
    );
  if (upcoming.length === 0)
    return (
      <EmptyState size="sm">
        <EmptyState.Header>
          <EmptyState.Media variant="icon">
            <FaIcon icon="calendar" aria-hidden="true" />
          </EmptyState.Media>
          <EmptyState.Title>
            {t("clinic.patient.reservations.empty")}
          </EmptyState.Title>
        </EmptyState.Header>
        <EmptyState.Content>
          <p className="text-sm text-muted">
            {t("clinic.patient.reservations.emptyHint")}
          </p>
          <Button variant="primary" onPress={onCreateReservation}>
            <FaIcon icon="plus" aria-hidden="true" />
            {t("common.actions.create")}
          </Button>
        </EmptyState.Content>
      </EmptyState>
    );

  return (
    <div className="flex justify-center">
      <Calendar
        aria-label={t("clinic.patient.reservations.upcoming")}
        defaultValue={focusedDate}
        focusedValue={focusedDate}
        onChange={(date) => {
          if (date)
            setDetailReservationId(
              dateToReservations.get(date.toString())?.id ?? null,
            );
        }}
      >
        <Calendar.Header>
          <Calendar.Heading />
          <Calendar.NavButton slot="previous" />
          <Calendar.NavButton slot="next" />
        </Calendar.Header>
        <Calendar.Grid>
          <Calendar.GridHeader>
            {(day) => <Calendar.HeaderCell>{day}</Calendar.HeaderCell>}
          </Calendar.GridHeader>
          <Calendar.GridBody>
            {(date) => (
              <Calendar.Cell date={date}>
                {({ formattedDate }) => (
                  <>
                    {formattedDate}
                    {dateToReservations.has(date.toString()) && (
                      <Calendar.CellIndicator />
                    )}
                  </>
                )}
              </Calendar.Cell>
            )}
          </Calendar.GridBody>
        </Calendar.Grid>
      </Calendar>
      {detailReservationId && (
        <ReservationDetailDialog
          isOpen
          onClose={() => setDetailReservationId(null)}
          reservationId={detailReservationId}
        />
      )}
    </div>
  );
}
