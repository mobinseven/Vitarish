"use client";

import { use } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@heroui/react";
import { Widget } from "@heroui-pro/react";
import { Routes } from "@/app/routes";
import { ClinicDetailGrid, type ClinicDetailItem } from "@vitarish/clinic-web";
import { ClinicErrorState, ClinicLoadingState } from "@vitarish/clinic-web";
import { useOwnerReservationDetail } from "@vitarish/clinic-web";
import { useLocalizedText, type MessageKey } from "@/localization";

const reservationStatusKeys: Record<number, MessageKey> = {
  0: "clinic.reservationStatus.pending",
  1: "clinic.reservationStatus.confirmed",
  2: "clinic.reservationStatus.cancelled",
  3: "clinic.reservationStatus.rejected",
  4: "clinic.reservationStatus.expired",
  5: "clinic.reservationStatus.waitlisted",
  6: "clinic.reservationStatus.present",
  7: "clinic.reservationStatus.completed",
};

export default function ClinicOwnerReservationDetailPage({
  params,
}: {
  params: Promise<{ reservationId: string }>;
}) {
  const { reservationId } = use(params);
  const router = useRouter();
  const { t } = useLocalizedText();
  const { data: reservation, isLoading, error } = useOwnerReservationDetail(reservationId);

  if (isLoading) {
    return <ClinicLoadingState label={t("common.states.loading")} />;
  }

  if (error || !reservation) {
    return <ClinicErrorState title={t("clinic.appointments.reservations.notFound")} />;
  }

  const details: ClinicDetailItem[] = [
    { label: t("clinic.fields.status"), value: t(reservationStatusKeys[reservation.status] ?? "common.values.unknown") },
    { label: t("clinic.fields.date"), value: reservation.date },
    { label: t("clinic.fields.shiftId"), value: reservation.shiftId.slice(0, 8) },
    { label: t("clinic.fields.hold"), value: reservation.isHold ? t("clinic.patient.overview.yes") : t("clinic.patient.overview.no") },
    ...(reservation.estimatedSessionLength
      ? [{ label: t("clinic.fields.sessionLength"), value: reservation.estimatedSessionLength }]
      : []),
    ...(reservation.confirmedAt
      ? [{ label: t("clinic.fields.confirmedAt"), value: new Date(reservation.confirmedAt).toLocaleString() }]
      : []),
    ...(reservation.cancelledAt
      ? [{ label: t("clinic.fields.cancelledAt"), value: new Date(reservation.cancelledAt).toLocaleString() }]
      : []),
    { label: t("clinic.fields.created"), value: new Date(reservation.createdAt).toLocaleString() },
  ];

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Button variant="ghost" className="mb-2" onPress={() => router.push(Routes.Clinic.Appointments.Reservations.Index)}>
          <i aria-hidden className="fa-solid fa-arrow-left" />
          {t("common.actions.previous")}
        </Button>
        <h1 className="text-2xl font-bold">{t("clinic.appointments.reservations.detailTitle")}</h1>
      </div>

      <Widget>
        <Widget.Content>
          <ClinicDetailGrid items={details} />
        </Widget.Content>
      </Widget>
    </div>
  );
}
