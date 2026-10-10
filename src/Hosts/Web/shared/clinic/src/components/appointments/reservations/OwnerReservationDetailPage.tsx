"use client";
import { useState } from "react";
import { Button } from "@heroui/react";
import {
  DetailGrid,
  type DetailGridItem,
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
import { ReservationDeleteDialog } from "./ReservationDeleteDialog";
import {
  useDeleteReservation,
  useOwnerReservationDetail,
} from "../../../hooks/appointments/reservations/useOwnerReservations";

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

export interface OwnerReservationDetailPageProps {
  reservationId: string;
  onNavigate: (intent: NavigationIntent) => void;
}

export function OwnerReservationDetailPage({
  reservationId,
  onNavigate,
}: OwnerReservationDetailPageProps) {
  const { t, locale } = useLocalizedText();
  const query = useOwnerReservationDetail(reservationId);
  const deletion = useDeleteReservation();
  const [deleting, setDeleting] = useState(false);

  if (query.isLoading)
    return <ClinicLoadingState label={t("common.states.loading")} />;
  if (query.error || !query.data)
    return (
      <ClinicErrorState
        title={t("clinic.appointments.reservations.notFound")}
      />
    );

  const reservation = query.data;
  const items: DetailGridItem[] = [
    {
      label: t("clinic.fields.status"),
      value: t(statusKeys[reservation.status] ?? "common.values.unknown"),
    },
    { label: t("clinic.fields.date"), value: reservation.date },
    {
      label: t("clinic.fields.shiftId"),
      value: reservation.shiftId.slice(0, 8),
    },
    {
      label: t("clinic.fields.hold"),
      value: reservation.isHold
        ? t("clinic.patient.overview.yes")
        : t("clinic.patient.overview.no"),
    },
    ...(reservation.estimatedSessionLength
      ? [
          {
            label: t("clinic.fields.sessionLength"),
            value: reservation.estimatedSessionLength,
          },
        ]
      : []),
    ...(reservation.confirmedAt
      ? [
          {
            label: t("clinic.fields.confirmedAt"),
            value: formatDateTime(reservation.confirmedAt, locale),
          },
        ]
      : []),
    ...(reservation.cancelledAt
      ? [
          {
            label: t("clinic.fields.cancelledAt"),
            value: formatDateTime(reservation.cancelledAt, locale),
          },
        ]
      : []),
    {
      label: t("clinic.fields.created"),
      value: formatDateTime(reservation.createdAt, locale),
    },
  ];

  return (
    <section className="flex flex-col gap-6">
      <header className="flex items-center justify-between">
        <div>
          <Button
            variant="ghost"
            onPress={() => onNavigate({ kind: "ownerReservationIndex" })}
          >
            {t("common.actions.previous")}
          </Button>
          <div className="flex items-center gap-4">
            <HeaderIcon icon="calendar-check" level="page" />
            <div className="min-w-0">
              <h1 className="text-2xl font-bold">
                {t("clinic.appointments.reservations.detailTitle")}
              </h1>
            </div>
          </div>
        </div>
        <Button variant="danger" onPress={() => setDeleting(true)}>
          {t("common.actions.delete")}
        </Button>
      </header>
      <DetailGrid items={items} />
      {deletion.isError ? (
        <ClinicErrorState
          title={t("clinic.appointments.reservations.deleteFailed")}
        />
      ) : null}
      <ReservationDeleteDialog
        isOpen={deleting}
        isPending={deletion.isPending}
        onCancel={() => setDeleting(false)}
        onConfirm={() =>
          deletion.mutate(reservationId, {
            onSuccess: () => onNavigate({ kind: "ownerReservationIndex" }),
          })
        }
      />
    </section>
  );
}
