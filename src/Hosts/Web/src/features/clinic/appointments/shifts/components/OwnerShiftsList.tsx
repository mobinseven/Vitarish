"use client";

import { useCallback } from "react";
import { useRouter } from "next/navigation";
import { Button, Spinner } from "@heroui/react";
import { ListView } from "@heroui-pro/react";
import { Routes } from "@/app/routes";
import { useOwnerShifts } from "../hooks/useOwnerShifts";
import { useLocalizedText } from "@/localization";

const DAY_NAMES = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

export function OwnerShiftsList() {
  const router = useRouter();
  const { t } = useLocalizedText();
  const { data: shifts, isLoading, error } = useOwnerShifts();

  const handleSelect = useCallback(
    (id: string) => router.push(Routes.Clinic.Appointments.Shifts.Detail(id)),
    [router]
  );

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">{t("clinic.appointments.shifts.title")}</h1>
        <Button variant="primary" onPress={() => router.push(Routes.Clinic.Appointments.Shifts.New)}>
          {t("clinic.appointments.shifts.new")}
        </Button>
      </div>

      {isLoading && (
        <div className="flex items-center justify-center gap-2 rounded-2xl bg-surface p-8 shadow-surface">
          <Spinner size="sm" />
          <span className="text-muted">{t("common.states.loading")}</span>
        </div>
      )}

      {error && (
        <div className="rounded-2xl bg-surface p-8 shadow-surface">
          <p className="text-sm text-danger">{t("clinic.appointments.shifts.loadError")}</p>
        </div>
      )}

      {!isLoading && !error && shifts && (
        <ListView
          aria-label={t("clinic.appointments.shifts.title")}
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
                    <ListView.Title>{DAY_NAMES[s.dayOfWeek]} {s.startTime} - {s.endTime}</ListView.Title>
                    <ListView.Description>{s.totalSeats} seats</ListView.Description>
                  </div>
                  <span className="text-xs text-muted">{s.serviceId ? "Linked" : "General"}</span>
                </div>
              </ListView.ItemContent>
            </ListView.Item>
          ))}
        </ListView>
      )}
    </div>
  );
}
