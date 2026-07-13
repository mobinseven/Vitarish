"use client";

import { useCallback } from "react";
import { ListView } from "@heroui-pro/react";
import type { ClinicNavigationIntent } from "../../../navigation";
import { EmptyStateSurface as ClinicEmptyState, ErrorStateSurface as ClinicErrorState, LoadingStateSurface as ClinicLoadingState } from "@vitarish/web-ui";
import { useOwnerShifts } from "../../../hooks/appointments/shifts/useOwnerShifts";
import { useLocalizedText, type MessageKey } from "../../../localization/index";

const dayNameKeys = [
  "clinic.weekdays.sunday",
  "clinic.weekdays.monday",
  "clinic.weekdays.tuesday",
  "clinic.weekdays.wednesday",
  "clinic.weekdays.thursday",
  "clinic.weekdays.friday",
  "clinic.weekdays.saturday",
] satisfies MessageKey[];

interface UserShiftsListProps {
  onNavigate?: (intent: ClinicNavigationIntent) => void;
}

export function UserShiftsList({ onNavigate }: UserShiftsListProps) {
  const { t } = useLocalizedText();
  const { data: shifts, isLoading, error } = useOwnerShifts({ visibility: 0 });

  const handleSelect = useCallback(
    (id: string) => onNavigate?.({ kind: "userShiftDetail", shiftId: id }),
    [onNavigate]
  );

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">{t("clinic.userAppointments.shifts.title")}</h1>
        <p className="text-sm text-muted">{t("clinic.userAppointments.shifts.description")}</p>
      </div>

      {isLoading && (
        <ClinicLoadingState label={t("common.states.loading")} />
      )}

      {error && (
        <ClinicErrorState title={t("clinic.appointments.shifts.failedToLoad")} />
      )}

      {!isLoading && !error && shifts && (
        <ListView
          aria-label={t("clinic.userAppointments.shifts.title")}
          onAction={(key) => handleSelect(String(key))}
          renderEmptyState={() => (
            <ClinicEmptyState title={t("clinic.appointments.shifts.empty")} />
          )}
        >
          {shifts.map((s) => (
            <ListView.Item key={s.id} id={s.id} textValue={`${t(dayNameKeys[s.dayOfWeek])} ${s.startTime}`}>
              <ListView.ItemContent>
                <div className="flex flex-col gap-1">
                  <ListView.Title>{t(dayNameKeys[s.dayOfWeek])} {s.startTime} - {s.endTime}</ListView.Title>
                  <ListView.Description>{t("clinic.fields.availableSeats")}: {s.totalSeats}</ListView.Description>
                </div>
              </ListView.ItemContent>
              <ListView.ItemAction>
                <span className="text-xs text-muted">{t("clinic.appointments.shifts.book")}</span>
              </ListView.ItemAction>
            </ListView.Item>
          ))}
        </ListView>
      )}
    </div>
  );
}
