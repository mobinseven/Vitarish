"use client";

import { use } from "react";
import { useRouter } from "next/navigation";
import { Card, Button, Spinner } from "@heroui/react";
import { Routes } from "@/app/routes";
import { useOwnerReservationDetail } from "@/features/clinic/appointments/reservations/hooks/useOwnerReservations";
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

export default function ClinicUserReservationDetailPage({
  params,
}: {
  params: Promise<{ reservationId: string }>;
}) {
  const { reservationId } = use(params);
  const router = useRouter();
  const { t } = useLocalizedText();
  const { data: reservation, isLoading, error } = useOwnerReservationDetail(reservationId);

  if (isLoading) {
    return (
      <Card className="p-8">
        <div className="flex items-center justify-center gap-2">
          <Spinner size="sm" />
          <span className="text-muted">{t("common.states.loading")}</span>
        </div>
      </Card>
    );
  }

  if (error || !reservation) {
    return (
      <Card className="p-8">
        <p className="text-danger">{error?.message ?? t("clinic.appointments.reservations.notFound")}</p>
      </Card>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Button variant="ghost" className="mb-2" onPress={() => router.push(Routes.ClinicUser.Reservations.Index)}>
          ← {t("common.actions.previous")}
        </Button>
        <h1 className="text-2xl font-bold">{t("clinic.userAppointments.reservations.detailTitle")}</h1>
      </div>

      <Card>
        <Card.Content className="grid gap-4 sm:grid-cols-2">
          <DetailField label={t("clinic.fields.status")} value={t(reservationStatusKeys[reservation.status] ?? "common.values.unknown")} />
          <DetailField label={t("clinic.fields.date")} value={reservation.date} />
          <DetailField label={t("clinic.fields.shift")} value={reservation.shiftId.slice(0, 8)} />
          <DetailField label={t("clinic.fields.created")} value={new Date(reservation.createdAt).toLocaleString()} />
        </Card.Content>
      </Card>
    </div>
  );
}

function DetailField({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex flex-col gap-1">
      <span className="text-xs text-muted">{label}</span>
      <span className="text-sm">{value}</span>
    </div>
  );
}
