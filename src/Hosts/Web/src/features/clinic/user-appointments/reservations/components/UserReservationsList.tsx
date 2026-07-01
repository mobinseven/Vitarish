"use client";

import { useCallback } from "react";
import { useRouter } from "next/navigation";
import { Card, Spinner } from "@heroui/react";
import { Routes } from "@/app/routes";
import { useOwnerReservations } from "@/features/clinic/appointments/reservations/hooks/useOwnerReservations";
import { useLocalizedText } from "@/localization";

const STATUS_LABELS: Record<number, string> = {
  0: "Pending", 1: "Confirmed", 2: "Cancelled", 3: "Rejected",
  4: "Expired", 5: "Waitlisted", 6: "Present", 7: "Completed",
};

export function UserReservationsList() {
  const router = useRouter();
  const { t } = useLocalizedText();
  const { data: reservations, isLoading, error } = useOwnerReservations();

  const handleSelect = useCallback(
    (id: string) => router.push(Routes.ClinicUser.Reservations.Detail(id)),
    [router]
  );

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">{t("clinic.userAppointments.reservations.title")}</h1>
        <p className="text-sm text-default-500">{t("clinic.userAppointments.reservations.description")}</p>
      </div>

      {isLoading && (
        <Card className="p-8">
          <div className="flex items-center justify-center gap-2">
            <Spinner size="sm" />
            <span className="text-default-500">{t("common.states.loading")}</span>
          </div>
        </Card>
      )}

      {error && (
        <Card className="p-8">
          <p className="text-sm text-danger">Failed to load reservations.</p>
        </Card>
      )}

      {!isLoading && !error && reservations && (
        <Card>
          <Card.Content className="flex flex-col gap-1">
            {reservations.length === 0 && (
              <p className="py-8 text-center text-sm text-default-500">No reservations found.</p>
            )}
            {reservations.map((r) => (
              <button
                key={r.id}
                onClick={() => handleSelect(r.id)}
                className="flex items-center justify-between rounded-lg px-4 py-3 text-start transition-colors hover:bg-default-100"
              >
                <div className="flex flex-col gap-1">
                  <span className="text-sm font-medium">{r.date}</span>
                  <span className="text-xs text-default-500">Shift: {r.shiftId.slice(0, 8)}</span>
                </div>
                <span className="text-xs text-default-500">{STATUS_LABELS[r.status] ?? "Unknown"}</span>
              </button>
            ))}
          </Card.Content>
        </Card>
      )}
    </div>
  );
}
