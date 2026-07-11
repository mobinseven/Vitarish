"use client";

import { useOwnerReservations } from "../../reservations/hooks/useOwnerReservations";
import { useOwnerShifts } from "../../shifts/hooks/useOwnerShifts";
import { ClinicErrorState, ClinicLoadingState } from "../../../components/ClinicStateSurface";
import { useLocalizedText } from "../../../../../localization";
import { AppointmentsAgenda } from "./AppointmentsAgenda";

export function OwnerReservationsAgenda() {
  const { t } = useLocalizedText();
  const { data: reservations, isLoading: reservationsLoading, error: reservationsError } = useOwnerReservations();
  const { data: shifts, isLoading: shiftsLoading, error: shiftsError } = useOwnerShifts();

  const isLoading = reservationsLoading || shiftsLoading;
  const error = reservationsError || shiftsError;

  return (
    <div className="flex flex-col gap-6">
      {isLoading && <ClinicLoadingState label={t("common.states.loading")} />}
      {error && <ClinicErrorState title={t("clinic.appointments.reservations.failedToLoad")} />}
      {!isLoading && !error && shifts && reservations && (
        <AppointmentsAgenda shifts={shifts} reservations={reservations} />
      )}
    </div>
  );
}
