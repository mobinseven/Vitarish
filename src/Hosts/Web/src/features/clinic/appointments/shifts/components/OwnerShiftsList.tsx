"use client";

import { useCallback } from "react";
import { useRouter } from "next/navigation";
import { Card, Button, Spinner } from "@heroui/react";
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
        <Card className="p-8">
          <div className="flex items-center justify-center gap-2">
            <Spinner size="sm" />
            <span className="text-default-500">{t("common.states.loading")}</span>
          </div>
        </Card>
      )}

      {error && (
        <Card className="p-8">
          <p className="text-sm text-danger">{t("clinic.appointments.shifts.loadError")}</p>
        </Card>
      )}

      {!isLoading && !error && shifts && (
        <Card>
          <Card.Content className="flex flex-col gap-1">
            {shifts.length === 0 && (
              <p className="py-8 text-center text-sm text-default-500">
                {t("clinic.appointments.shifts.empty")}
              </p>
            )}
            {shifts.map((s) => (
              <button
                key={s.id}
                onClick={() => handleSelect(s.id)}
                className="flex items-center justify-between rounded-lg px-4 py-3 text-start transition-colors hover:bg-default-100"
              >
                <div className="flex flex-col gap-1">
                  <span className="text-sm font-medium">{DAY_NAMES[s.dayOfWeek]} {s.startTime} - {s.endTime}</span>
                  <span className="text-xs text-default-500">{s.totalSeats} seats</span>
                </div>
                <span className="text-xs text-default-500">{s.serviceId ? "Linked" : "General"}</span>
              </button>
            ))}
          </Card.Content>
        </Card>
      )}
    </div>
  );
}
