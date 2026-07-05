"use client";

import { use } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@heroui/react";
import { Widget } from "@heroui-pro/react";
import { Routes } from "@/app/routes";
import { ClinicDetailGrid } from "@/features/clinic/components/ClinicDetailGrid";
import { ClinicErrorState, ClinicLoadingState } from "@/features/clinic/components/ClinicStateSurface";
import { useOwnerShiftDetail } from "@/features/clinic/appointments/shifts/hooks/useOwnerShifts";
import { useLocalizedText, formatDateTime, type MessageKey } from "@/localization";

const dayNameKeys = [
  "clinic.weekdays.sunday",
  "clinic.weekdays.monday",
  "clinic.weekdays.tuesday",
  "clinic.weekdays.wednesday",
  "clinic.weekdays.thursday",
  "clinic.weekdays.friday",
  "clinic.weekdays.saturday",
] satisfies MessageKey[];

export default function ClinicUserShiftDetailPage({
  params,
}: {
  params: Promise<{ shiftId: string }>;
}) {
  const { shiftId } = use(params);
  const router = useRouter();
  const { t, locale } = useLocalizedText();
  const { data: shift, isLoading, error } = useOwnerShiftDetail(shiftId);
  const dayName = shift ? t(dayNameKeys[shift.dayOfWeek]) : "";

  if (isLoading) {
    return <ClinicLoadingState label={t("common.states.loading")} />;
  }

  if (error || !shift) {
    return <ClinicErrorState title={t("clinic.appointments.shifts.notFound")} />;
  }

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Button variant="ghost" className="mb-2" onPress={() => router.push(Routes.ClinicUser.Shifts.Index)}>
          <i aria-hidden className="fa-solid fa-arrow-left" />
          {t("common.actions.previous")}
        </Button>
        <h1 className="text-2xl font-bold">{dayName} {shift.startTime} - {shift.endTime}</h1>
      </div>

      <Widget>
        <Widget.Content>
          <ClinicDetailGrid
            items={[
              { label: t("clinic.fields.day"), value: dayName },
              { label: t("clinic.fields.start"), value: shift.startTime },
              { label: t("clinic.fields.end"), value: shift.endTime },
              { label: t("clinic.fields.availableSeats"), value: shift.totalSeats },
              { label: t("clinic.fields.timezone"), value: shift.timeZone },
              { label: t("clinic.fields.created"), value: formatDateTime(shift.createdAt, locale) },
            ]}
          />
        </Widget.Content>
      </Widget>
    </div>
  );
}
