"use client";

import { useCallback } from "react";
import { Button } from "@heroui/react";
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

interface OwnerShiftsListProps {
  onNavigate?: (intent: ClinicNavigationIntent) => void;
}

export function OwnerShiftsList({ onNavigate }: OwnerShiftsListProps) {
  const { t } = useLocalizedText();
  const { data: shifts, isLoading, error } = useOwnerShifts();

  const handleSelect = useCallback(
    (id: string) => onNavigate?.({ kind: "ownerShiftDetail", shiftId: id }),
    [onNavigate]
  );

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">{t("clinic.appointments.shifts.title")}</h1>
        <Button variant="primary" onPress={() => onNavigate?.({ kind: "ownerShiftCreate" })}>
          {t("clinic.appointments.shifts.new")}
        </Button>
      </div>

      {isLoading && (
        <ClinicLoadingState label={t("common.states.loading")} />
      )}

      {error && (
        <ClinicErrorState title={t("clinic.appointments.shifts.failedToLoad")} />
      )}

      {!isLoading && !error && shifts && (
        <ListView
          aria-label={t("clinic.appointments.shifts.title")}
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
                  <ListView.Description>{t("clinic.fields.totalSeats")}: {s.totalSeats}</ListView.Description>
                </div>
              </ListView.ItemContent>
              <ListView.ItemAction>
                <span className="text-xs text-muted">
                  {s.serviceId ? t("clinic.appointments.shifts.linked") : t("clinic.appointments.shifts.general")}
                </span>
              </ListView.ItemAction>
            </ListView.Item>
          ))}
        </ListView>
      )}
    </div>
  );
}
