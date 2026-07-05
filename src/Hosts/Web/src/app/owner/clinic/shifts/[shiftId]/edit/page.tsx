"use client";

import { use, useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { Button, Input, Label, TextField } from "@heroui/react";
import { NativeSelect, NumberStepper, Widget } from "@heroui-pro/react";
import { Routes } from "@/app/routes";
import { ClinicLoadingState } from "@/features/clinic/components/ClinicStateSurface";
import { useOwnerShiftDetail, useUpdateShift } from "@/features/clinic/appointments/shifts/hooks/useOwnerShifts";
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

export default function ClinicOwnerShiftEditPage({
  params,
}: {
  params: Promise<{ shiftId: string }>;
}) {
  const { shiftId } = use(params);
  const router = useRouter();
  const { t } = useLocalizedText();
  const { data: shift, isLoading } = useOwnerShiftDetail(shiftId);
  const updateShift = useUpdateShift();

  const [dayOfWeek, setDayOfWeek] = useState("1");
  const [startTime, setStartTime] = useState("09:00");
  const [endTime, setEndTime] = useState("10:00");
  const [totalSeats, setTotalSeats] = useState(10);

  useEffect(() => {
    if (shift) {
      setDayOfWeek(String(shift.dayOfWeek));
      setStartTime(shift.startTime);
      setEndTime(shift.endTime);
      setTotalSeats(shift.totalSeats);
    }
  }, [shift]);

  const handleSubmit = useCallback(() => {
    updateShift.mutate(
      {
        id: shiftId,
        dayOfWeek: Number(dayOfWeek),
        startTime,
        endTime,
        totalSeats,
        timeZone: "UTC",
      },
      {
        onSuccess: (result) => {
          if (result.succeeded) {
            router.push(Routes.Clinic.Appointments.Shifts.Detail(shiftId));
          }
        },
      }
    );
  }, [shiftId, dayOfWeek, startTime, endTime, totalSeats, updateShift, router]);

  if (isLoading) {
    return <ClinicLoadingState label={t("common.states.loading")} />;
  }

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Button variant="ghost" className="mb-2" onPress={() => router.push(Routes.Clinic.Appointments.Shifts.Detail(shiftId))}>
          <i aria-hidden className="fa-solid fa-arrow-left" />
          {t("common.actions.previous")}
        </Button>
        <h1 className="text-2xl font-bold">{t("clinic.appointments.shifts.editTitle")}</h1>
      </div>

      <Widget>
        <Widget.Content className="flex flex-col gap-4">
          <NativeSelect fullWidth>
            <Label>{t("clinic.fields.dayOfWeek")}</Label>
            <NativeSelect.Trigger
              name="dayOfWeek"
              value={dayOfWeek}
              onChange={(event) => setDayOfWeek(event.target.value)}
            >
              {dayNameKeys.map((key, index) => (
                <NativeSelect.Option key={String(index)} value={String(index)}>
                  {t(key)}
                </NativeSelect.Option>
              ))}
              <NativeSelect.Indicator />
            </NativeSelect.Trigger>
          </NativeSelect>

          <div className="grid gap-4 sm:grid-cols-2">
            <TextField>
              <Label>{t("clinic.fields.startTime")}</Label>
              <Input type="time" value={startTime} onChange={(event) => setStartTime(event.target.value)} />
            </TextField>
            <TextField>
              <Label>{t("clinic.fields.endTime")}</Label>
              <Input type="time" value={endTime} onChange={(event) => setEndTime(event.target.value)} />
            </TextField>
          </div>

          <div className="flex flex-col gap-2">
            <Label>{t("clinic.fields.totalSeats")}</Label>
            <NumberStepper
              aria-label={t("clinic.fields.totalSeats")}
              minValue={1}
              value={totalSeats}
              onChange={setTotalSeats}
            >
              <NumberStepper.Group>
                <NumberStepper.DecrementButton />
                <NumberStepper.Value />
                <NumberStepper.IncrementButton />
              </NumberStepper.Group>
            </NumberStepper>
          </div>

          <div className="flex justify-end gap-2">
            <Button variant="ghost" onPress={() => router.push(Routes.Clinic.Appointments.Shifts.Detail(shiftId))}>
              {t("common.actions.cancel")}
            </Button>
            <Button variant="primary" isPending={updateShift.isPending} onPress={handleSubmit}>
              {t("common.actions.save")}
            </Button>
          </div>
        </Widget.Content>
      </Widget>
    </div>
  );
}
