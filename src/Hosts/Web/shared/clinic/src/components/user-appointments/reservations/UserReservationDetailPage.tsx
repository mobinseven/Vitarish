"use client";
import { Button } from "@heroui/react";
import {
  DetailGrid,
  HeaderIcon,
} from "@vitarish/ui";
import type { NavigationIntent } from "../../../navigation";
import {
  formatDateTime,
  useLocalizedText,
  type MessageKey,
} from "../../../localization/index";
import {
  ErrorStateSurface as ClinicErrorState,
  LoadingStateSurface as ClinicLoadingState,
} from "@vitarish/ui";
import { useOwnerReservationDetail } from "../../../hooks/appointments/reservations/useOwnerReservations";

const statusKeys: Record<number, MessageKey> = {
  0: "clinic.reservationStatus.pending",
  1: "clinic.reservationStatus.confirmed",
  2: "clinic.reservationStatus.cancelled",
  3: "clinic.reservationStatus.rejected",
  4: "clinic.reservationStatus.expired",
  5: "clinic.reservationStatus.waitlisted",
  6: "clinic.reservationStatus.present",
  7: "clinic.reservationStatus.completed",
};

export interface UserReservationDetailPageProps {
  reservationId: string;
  onNavigate: (intent: NavigationIntent) => void;
}

export function UserReservationDetailPage({
  reservationId,
  onNavigate,
}: UserReservationDetailPageProps) {
  const { t, locale } = useLocalizedText();
  const query = useOwnerReservationDetail(reservationId);
  if (query.isLoading)
    return <ClinicLoadingState label={t("common.states.loading")} />;
  if (query.error || !query.data)
    return (
      <ClinicErrorState
        title={t("clinic.appointments.reservations.notFound")}
      />
    );
  const reservation = query.data;
  return (
    <section className="flex flex-col gap-6">
      <header>
        <Button
          variant="ghost"
          onPress={() => onNavigate({ kind: "userReservationIndex" })}
        >
          {t("common.actions.previous")}
        </Button>
        <div className="flex items-center gap-4">
          <HeaderIcon icon="calendar-check" level="page" />
          <div className="min-w-0">
            <h1 className="text-2xl font-bold">
              {t("clinic.userAppointments.reservations.detailTitle")}
            </h1>
          </div>
        </div>
      </header>
      <DetailGrid
        items={[
          {
            label: t("clinic.fields.status"),
            value: t(statusKeys[reservation.status] ?? "common.values.unknown"),
          },
          { label: t("clinic.fields.date"), value: reservation.date },
          {
            label: t("clinic.fields.shift"),
            value: reservation.shiftId.slice(0, 8),
          },
          {
            label: t("clinic.fields.created"),
            value: formatDateTime(reservation.createdAt, locale),
          },
        ]}
      />
    </section>
  );
}
