"use client";

import { use } from "react";
import { useRouter } from "next/navigation";
import { Card, Button, Spinner } from "@heroui/react";
import { Routes } from "@/app/routes";
import { useOwnerReservationDetail } from "@/features/clinic/appointments/reservations/hooks/useOwnerReservations";
import { useLocalizedText } from "@/localization";

const STATUS_LABELS: Record<number, string> = {
  0: "Pending", 1: "Confirmed", 2: "Cancelled", 3: "Rejected",
  4: "Expired", 5: "Waitlisted", 6: "Present", 7: "Completed",
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
          <span className="text-default-500">{t("common.states.loading")}</span>
        </div>
      </Card>
    );
  }

  if (error || !reservation) {
    return (
      <Card className="p-8">
        <p className="text-danger">{error?.message ?? "Reservation not found."}</p>
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
          <DetailField label="Status" value={STATUS_LABELS[reservation.status] ?? "Unknown"} />
          <DetailField label="Date" value={reservation.date} />
          <DetailField label="Shift" value={reservation.shiftId.slice(0, 8)} />
          <DetailField label="Created" value={new Date(reservation.createdAt).toLocaleString()} />
        </Card.Content>
      </Card>
    </div>
  );
}

function DetailField({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex flex-col gap-1">
      <span className="text-xs text-default-500">{label}</span>
      <span className="text-sm">{value}</span>
    </div>
  );
}
