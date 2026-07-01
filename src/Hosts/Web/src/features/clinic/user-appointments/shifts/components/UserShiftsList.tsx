"use client";

import { useCallback } from "react";
import { useRouter } from "next/navigation";
import { Card, Spinner } from "@heroui/react";
import { Routes } from "@/app/routes";
import { useOwnerShifts } from "@/features/clinic/appointments/shifts/hooks/useOwnerShifts";
import { useLocalizedText } from "@/localization";

const DAY_NAMES = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

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
        <p className="text-sm text-default-500">{t("clinic.userAppointments.shifts.description")}</p>
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
          <p className="text-sm text-danger">Failed to load shifts.</p>
        </Card>
      )}

      {!isLoading && !error && shifts && (
        <Card>
          <Card.Content className="flex flex-col gap-1">
            {shifts.length === 0 && (
              <p className="py-8 text-center text-sm text-default-500">No shifts available.</p>
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
                <span className="text-xs text-default-500">Book</span>
              </button>
            ))}
          </Card.Content>
        </Card>
      )}
    </div>
  );
}
