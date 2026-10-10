"use client";
import { Button } from "@heroui/react";
import {
  DetailGrid,
  HeaderIcon,
} from "@vitarish/ui";
import type { NavigationIntent } from "../../../navigation";
import {
  formatDateTime,
  useLocalizedText,
  type MessageKey,
} from "../../../localization/index";
import {
  ErrorStateSurface as ClinicErrorState,
  LoadingStateSurface as ClinicLoadingState,
} from "@vitarish/ui";
import { useOwnerShiftDetail } from "../../../hooks/appointments/shifts/useOwnerShifts";
const dayKeys = [
  "clinic.weekdays.sunday",
  "clinic.weekdays.monday",
  "clinic.weekdays.tuesday",
  "clinic.weekdays.wednesday",
  "clinic.weekdays.thursday",
  "clinic.weekdays.friday",
  "clinic.weekdays.saturday",
] satisfies MessageKey[];
export interface UserShiftDetailPageProps {
  shiftId: string;
  onNavigate: (intent: NavigationIntent) => void;
}
export function UserShiftDetailPage({
  shiftId,
  onNavigate,
}: UserShiftDetailPageProps) {
  const { t, locale } = useLocalizedText();
  const query = useOwnerShiftDetail(shiftId);
  if (query.isLoading)
    return <ClinicLoadingState label={t("common.states.loading")} />;
  if (query.error || !query.data)
    return (
      <ClinicErrorState title={t("clinic.appointments.shifts.notFound")} />
    );
  const shift = query.data,
    day = t(dayKeys[shift.dayOfWeek] ?? "common.values.unknown");
  return (
    <section className="flex flex-col gap-6">
      <header>
        <Button
          variant="ghost"
          onPress={() => onNavigate({ kind: "userShiftIndex" })}
        >
          {t("common.actions.previous")}
        </Button>
        <div className="flex items-center gap-4">
          <HeaderIcon context="scheduling" icon="clock" level="page" />
          <div className="min-w-0">
            <h1 className="text-2xl font-bold">
              {day} {shift.startTime} - {shift.endTime}
            </h1>
          </div>
        </div>
      </header>
      <DetailGrid
        items={[
          { label: t("clinic.fields.day"), value: day },
          { label: t("clinic.fields.start"), value: shift.startTime },
          { label: t("clinic.fields.end"), value: shift.endTime },
          { label: t("clinic.fields.availableSeats"), value: shift.totalSeats },
          { label: t("clinic.fields.timezone"), value: shift.timeZone },
          {
            label: t("clinic.fields.created"),
            value: formatDateTime(shift.createdAt, locale),
          },
        ]}
      />
    </section>
  );
}
