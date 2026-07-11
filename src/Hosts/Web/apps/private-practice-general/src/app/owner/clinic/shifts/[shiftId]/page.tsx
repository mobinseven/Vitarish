"use client";

import { use } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@heroui/react";
import { Widget } from "@heroui-pro/react";
import { Routes } from "@/app/routes";
import { ClinicDetailGrid } from "@vitarish/clinic-web";
import { ClinicErrorState, ClinicLoadingState } from "@vitarish/clinic-web";
import { useOwnerShiftDetail, useDeleteShift } from "@vitarish/clinic-web";
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

export default function ClinicOwnerShiftDetailPage({
  params,
}: {
  params: Promise<{ shiftId: string }>;
}) {
  const { shiftId } = use(params);
  const router = useRouter();
  const { t, locale } = useLocalizedText();
  const { data: shift, isLoading, error } = useOwnerShiftDetail(shiftId);
  const deleteShift = useDeleteShift();
  const dayName = shift ? t(dayNameKeys[shift.dayOfWeek]) : "";

  if (isLoading) {
    return <ClinicLoadingState label={t("common.states.loading")} />;
  }

  if (error || !shift) {
    return <ClinicErrorState title={t("clinic.appointments.shifts.notFound")} />;
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <div>
          <Button variant="ghost" className="mb-2" onPress={() => router.push(Routes.Clinic.Appointments.Shifts.Index)}>
            <i aria-hidden className="fa-solid fa-arrow-left" />
            {t("common.actions.previous")}
          </Button>
          <h1 className="text-2xl font-bold">{dayName} {shift.startTime} - {shift.endTime}</h1>
        </div>
        <div className="flex gap-2">
          <Button variant="ghost" onPress={() => router.push(Routes.Clinic.Appointments.Shifts.Edit(shiftId))}>
            {t("common.actions.edit")}
          </Button>
          <Button
            variant="danger"
            isPending={deleteShift.isPending}
            onPress={() => {
              if (confirm(t("clinic.appointments.shifts.confirmDelete"))) {
                deleteShift.mutate(shiftId, {
                  onSuccess: () => router.push(Routes.Clinic.Appointments.Shifts.Index),
                });
              }
            }}
          >
            {t("common.actions.delete")}
          </Button>
        </div>
      </div>

      <Widget>
        <Widget.Content>
          <ClinicDetailGrid
            items={[
              { label: t("clinic.fields.day"), value: dayName },
              { label: t("clinic.fields.start"), value: shift.startTime },
              { label: t("clinic.fields.end"), value: shift.endTime },
              { label: t("clinic.fields.totalSeats"), value: shift.totalSeats },
              { label: t("clinic.fields.timezone"), value: shift.timeZone },
              { label: t("clinic.fields.created"), value: formatDateTime(shift.createdAt, locale) },
            ]}
          />
        </Widget.Content>
      </Widget>
    </div>
  );
}
