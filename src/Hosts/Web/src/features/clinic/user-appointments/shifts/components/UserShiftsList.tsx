"use client";

import { useCallback } from "react";
import { useRouter } from "next/navigation";
import { Spinner } from "@heroui/react";
import { ListView } from "@heroui-pro/react";
import { Routes } from "@/app/routes";
import { useOwnerShifts } from "@/features/clinic/appointments/shifts/hooks/useOwnerShifts";
import { useLocalizedText, type MessageKey } from "@/localization";

const dayNameKeys = [
  "clinic.weekdays.sunday",
  "clinic.weekdays.monday",
  "clinic.weekdays.tuesday",
  "clinic.weekdays.wednesday",
  "clinic.weekdays.thursday",
  "clinic.weekdays.friday",
  "clinic.weekdays.saturday",
] satisfies MessageKey[];

export function UserShiftsList() {
  const router = useRouter();
  const { t } = useLocalizedText();
  const { data: shifts, isLoading, error } = useOwnerShifts({ visibility: 0 });

  const handleSelect = useCallback(
    (id: string) => router.push(Routes.ClinicUser.Shifts.Detail(id)),
    [router]
  );

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">{t("clinic.userAppointments.shifts.title")}</h1>
        <p className="text-sm text-muted">{t("clinic.userAppointments.shifts.description")}</p>
      </div>

      {isLoading && (
        <div className="flex items-center justify-center gap-2 rounded-2xl bg-surface p-8 shadow-surface">
          <Spinner size="sm" />
          <span className="text-muted">{t("common.states.loading")}</span>
        </div>
      )}

      {error && (
        <div className="rounded-2xl bg-surface p-8 shadow-surface">
          <p className="text-sm text-danger">{t("clinic.appointments.shifts.failedToLoad")}</p>
        </div>
      )}

      {!isLoading && !error && shifts && (
        <ListView
          aria-label={t("clinic.userAppointments.shifts.title")}
          onAction={(key) => handleSelect(String(key))}
          renderEmptyState={() => (
            <p className="py-8 text-center text-sm text-muted">
              {t("clinic.appointments.shifts.empty")}
            </p>
          )}
        >
          {shifts.map((s) => (
            <ListView.Item key={s.id} id={s.id}>
              <ListView.ItemContent>
                <div className="flex items-center justify-between w-full">
                  <div className="flex flex-col gap-1">
                    <ListView.Title>{t(dayNameKeys[s.dayOfWeek])} {s.startTime} - {s.endTime}</ListView.Title>
                    <ListView.Description>{t("clinic.fields.availableSeats")}: {s.totalSeats}</ListView.Description>
                  </div>
                  <span className="text-xs text-muted">{t("clinic.appointments.shifts.book")}</span>
                </div>
              </ListView.ItemContent>
            </ListView.Item>
          ))}
        </ListView>
      )}
    </div>
  );
}
