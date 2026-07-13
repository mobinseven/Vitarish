"use client";
import { Button } from "@heroui/react";
import { DetailGrid } from "@vitarish/web-ui";
import type { ClinicNavigationIntent } from "../../../../../navigation";
import { formatDateTime, useLocalizedText, type MessageKey } from "../../../../../localization";
import { ErrorStateSurface as ClinicErrorState, LoadingStateSurface as ClinicLoadingState } from "@vitarish/web-ui";
import { useOwnerReservationDetail } from "../../../appointments/reservations/hooks/useOwnerReservations";

const statusKeys: Record<number, MessageKey> = {
  0: "clinic.reservationStatus.pending", 1: "clinic.reservationStatus.confirmed",
  2: "clinic.reservationStatus.cancelled", 3: "clinic.reservationStatus.rejected",
  4: "clinic.reservationStatus.expired", 5: "clinic.reservationStatus.waitlisted",
  6: "clinic.reservationStatus.present", 7: "clinic.reservationStatus.completed",
};

export interface ClinicUserReservationDetailPageProps {
  reservationId: string;
  onNavigate: (intent: ClinicNavigationIntent) => void;
}

export function ClinicUserReservationDetailPage({ reservationId, onNavigate }: ClinicUserReservationDetailPageProps) {
  const { t, locale } = useLocalizedText();
  const query = useOwnerReservationDetail(reservationId);
  if (query.isLoading) return <ClinicLoadingState label={t("common.states.loading")} />;
  if (query.error || !query.data) return <ClinicErrorState title={t("clinic.appointments.reservations.notFound")} />;
  const reservation = query.data;
  return (
    <section className="flex flex-col gap-6">
      <header>
        <Button variant="ghost" onPress={() => onNavigate({ kind: "userReservationIndex" })}>{t("common.actions.previous")}</Button>
        <h1 className="text-2xl font-bold">{t("clinic.userAppointments.reservations.detailTitle")}</h1>
      </header>
      <DetailGrid items={[
        { label: t("clinic.fields.status"), value: t(statusKeys[reservation.status] ?? "common.values.unknown") },
        { label: t("clinic.fields.date"), value: reservation.date },
        { label: t("clinic.fields.shift"), value: reservation.shiftId.slice(0, 8) },
        { label: t("clinic.fields.created"), value: formatDateTime(reservation.createdAt, locale) },
      ]} />
    </section>
  );
}
