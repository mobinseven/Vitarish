"use client";

import { useCallback } from "react";
import { ListView } from "@heroui-pro/react";
import type { NavigationIntent } from "../../../navigation";
import {
  EmptyStateSurface as ClinicEmptyState,
  ErrorStateSurface as ClinicErrorState,
  LoadingStateSurface as ClinicLoadingState,
} from "@vitarish/ui";
import { useOwnerReservations } from "../../../hooks/appointments/reservations/useOwnerReservations";
import { useLocalizedText, type MessageKey } from "../../../localization/index";

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

interface UserReservationsListProps {
  onNavigate?: (intent: NavigationIntent) => void;
}

export function UserReservationsList({
  onNavigate,
}: UserReservationsListProps) {
  const { t } = useLocalizedText();
  const { data: reservations, isLoading, error } = useOwnerReservations();

  const handleSelect = useCallback(
    (id: string) =>
      onNavigate?.({ kind: "userReservationDetail", reservationId: id }),
    [onNavigate],
  );

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">
          {t("clinic.userAppointments.reservations.title")}
        </h1>
        <p className="text-sm text-muted">
          {t("clinic.userAppointments.reservations.description")}
        </p>
      </div>

      {isLoading && <ClinicLoadingState label={t("common.states.loading")} />}

      {error && (
        <ClinicErrorState
          title={t("clinic.appointments.reservations.failedToLoad")}
        />
      )}

      {!isLoading && !error && reservations && (
        <ListView
          aria-label={t("clinic.userAppointments.reservations.title")}
          onAction={(key) => handleSelect(String(key))}
          renderEmptyState={() => (
            <ClinicEmptyState
              title={t("clinic.appointments.reservations.empty")}
            />
          )}
        >
          {reservations.map((r) => (
            <ListView.Item key={r.id} id={r.id} textValue={r.date}>
              <ListView.ItemContent>
                <div className="flex flex-col gap-1">
                  <ListView.Title>{r.date}</ListView.Title>
                  <ListView.Description>
                    {t("clinic.fields.shift")}: {r.shiftId.slice(0, 8)}
                  </ListView.Description>
                </div>
              </ListView.ItemContent>
              <ListView.ItemAction>
                <span className="text-xs text-muted">
                  {t(
                    reservationStatusKeys[r.status] ?? "common.values.unknown",
                  )}
                </span>
              </ListView.ItemAction>
            </ListView.Item>
          ))}
        </ListView>
      )}
    </div>
  );
}
