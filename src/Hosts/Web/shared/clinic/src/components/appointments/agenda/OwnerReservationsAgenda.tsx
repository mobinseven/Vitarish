"use client";

import { useOwnerReservations } from "../../../hooks/appointments/reservations/useOwnerReservations";
import { useOwnerShifts } from "../../../hooks/appointments/shifts/useOwnerShifts";
import {
  ErrorStateSurface as ClinicErrorState,
  LoadingStateSurface as ClinicLoadingState,
} from "@vitarish/ui";
import type { NavigationIntent } from "../../../navigation";
import { useLocalizedText } from "../../../localization/index";
import { AppointmentsAgenda } from "./AppointmentsAgenda";

interface OwnerReservationsAgendaProps {
  onNavigate?: (intent: NavigationIntent) => void;
}

export function OwnerReservationsAgenda({
  onNavigate,
}: OwnerReservationsAgendaProps) {
  const { t } = useLocalizedText();
  const {
    data: reservations,
    isLoading: reservationsLoading,
    error: reservationsError,
  } = useOwnerReservations();
  const {
    data: shifts,
    isLoading: shiftsLoading,
    error: shiftsError,
  } = useOwnerShifts();

  const isLoading = reservationsLoading || shiftsLoading;
  const error = reservationsError || shiftsError;

  return (
    <div className="flex flex-col gap-6">
      {isLoading && <ClinicLoadingState label={t("common.states.loading")} />}
      {error && (
        <ClinicErrorState
          title={t("clinic.appointments.reservations.failedToLoad")}
        />
      )}
      {!isLoading && !error && shifts && reservations && (
        <AppointmentsAgenda
          shifts={shifts}
          reservations={reservations}
          onNavigate={onNavigate}
        />
      )}
    </div>
  );
}
