"use client";

import { use } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@heroui/react";
import { Widget } from "@heroui-pro/react";
import { Routes } from "@/app/routes";
import { ClinicDetailGrid } from "@/features/clinic/components/ClinicDetailGrid";
import { ClinicErrorState, ClinicLoadingState } from "@/features/clinic/components/ClinicStateSurface";
import { useOwnerReservationDetail } from "@/features/clinic/appointments/reservations/hooks/useOwnerReservations";
import { useLocalizedText, formatDateTime, type MessageKey } from "@/localization";

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

export default function ClinicUserReservationDetailPage({
  params,
}: {
  params: Promise<{ reservationId: string }>;
}) {
  const { reservationId } = use(params);
  const router = useRouter();
  const { t, locale } = useLocalizedText();
  const { data: reservation, isLoading, error } = useOwnerReservationDetail(reservationId);

  if (isLoading) {
    return <ClinicLoadingState label={t("common.states.loading")} />;
  }

  if (error || !reservation) {
    return <ClinicErrorState title={t("clinic.appointments.reservations.notFound")} />;
  }

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Button variant="ghost" className="mb-2" onPress={() => router.push(Routes.ClinicUser.Reservations.Index)}>
          <i aria-hidden className="fa-solid fa-arrow-left" />
          {t("common.actions.previous")}
        </Button>
        <h1 className="text-2xl font-bold">{t("clinic.userAppointments.reservations.detailTitle")}</h1>
      </div>

      <Widget>
        <Widget.Content>
          <ClinicDetailGrid
            items={[
              { label: t("clinic.fields.status"), value: t(reservationStatusKeys[reservation.status] ?? "common.values.unknown") },
              { label: t("clinic.fields.date"), value: reservation.date },
              { label: t("clinic.fields.shift"), value: reservation.shiftId.slice(0, 8) },
              { label: t("clinic.fields.created"), value: formatDateTime(reservation.createdAt, locale) },
            ]}
          />
        </Widget.Content>
      </Widget>
    </div>
  );
}
