"use client";

import { useCallback } from "react";
import { useRouter } from "next/navigation";
import { Spinner } from "@heroui/react";
import { ListView } from "@heroui-pro/react";
import { Routes } from "@/app/routes";
import { useOwnerReservations } from "../hooks/useOwnerReservations";
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
        <div className="flex items-center justify-center gap-2 rounded-2xl bg-surface p-8 shadow-surface">
          <Spinner size="sm" />
          <span className="text-muted">{t("common.states.loading")}</span>
        </div>
      )}

      {error && (
        <div className="rounded-2xl bg-surface p-8 shadow-surface">
          <p className="text-sm text-danger">{t("clinic.appointments.reservations.loadError")}</p>
        </div>
      )}

      {!isLoading && !error && reservations && (
        <ListView
          aria-label={t("clinic.appointments.reservations.title")}
          onAction={(key) => handleSelect(String(key))}
          renderEmptyState={() => (
            <p className="py-8 text-center text-sm text-muted">
              {t("clinic.appointments.reservations.empty")}
            </p>
          )}
        >
          {reservations.map((r) => (
            <ListView.Item key={r.id} id={r.id}>
              <ListView.ItemContent>
                <div className="flex items-center justify-between w-full">
                  <div className="flex flex-col gap-1">
                    <ListView.Title>{r.date}</ListView.Title>
                    <ListView.Description>Shift: {r.shiftId.slice(0, 8)}</ListView.Description>
                  </div>
                  <span className="text-xs text-muted">{STATUS_LABELS[r.status] ?? "Unknown"}</span>
                </div>
              </ListView.ItemContent>
            </ListView.Item>
          ))}
        </ListView>
      )}
    </div>
  );
}
