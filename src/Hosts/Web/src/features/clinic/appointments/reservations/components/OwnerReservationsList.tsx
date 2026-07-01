"use client";

import { useCallback } from "react";
import { useRouter } from "next/navigation";
import { Card, Button, Spinner } from "@heroui/react";
import { Routes } from "@/app/routes";
import { useOwnerReservations } from "../hooks/useOwnerReservations";
import type { ReservationReadDto } from "../../api/appointmentsApi";
import { useLocalizedText } from "@/localization";

const STATUS_LABELS: Record<number, string> = {
  0: "Pending", 1: "Confirmed", 2: "Cancelled", 3: "Rejected",
  4: "Expired", 5: "Waitlisted", 6: "Present", 7: "Completed",
};

export function OwnerReservationsList() {
  const router = useRouter();
  const { t } = useLocalizedText();
  const { data: reservations, isLoading, error } = useOwnerReservations();

  const handleSelect = useCallback(
    (id: string) => router.push(Routes.Clinic.Appointments.Reservations.Detail(id)),
    [router]
  );

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">{t("clinic.appointments.reservations.title")}</h1>
      </div>

      {isLoading && (
        <Card className="p-8">
          <div className="flex items-center justify-center gap-2">
            <Spinner size="sm" />
            <span className="text-default-500">{t("common.loading")}</span>
          </div>
        </Card>
      )}

      {error && (
        <Card className="p-8">
          <p className="text-sm text-danger">{t("clinic.appointments.reservations.loadError")}</p>
        </Card>
      )}

      {!isLoading && !error && reservations && (
        <Card>
          <Card.Content className="flex flex-col gap-1">
            {reservations.length === 0 && (
              <p className="py-8 text-center text-sm text-default-500">
                {t("clinic.appointments.reservations.empty")}
              </p>
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
