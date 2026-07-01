"use client";

import { use, useCallback, useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { Card, Button, TextField, Label, Input, Select, ListBox, Spinner } from "@heroui/react";
import { Routes } from "@/app/routes";
import { useOwnerShiftDetail, useUpdateShift } from "@/features/clinic/appointments/shifts/hooks/useOwnerShifts";
import { useLocalizedText } from "@/localization";

const DAY_NAMES = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

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
  const [totalSeats, setTotalSeats] = useState("10");

  useEffect(() => {
    if (shift) {
      setDayOfWeek(String(shift.dayOfWeek));
      setStartTime(shift.startTime);
      setEndTime(shift.endTime);
      setTotalSeats(String(shift.totalSeats));
    }
  }, [shift]);

  const handleSubmit = useCallback(() => {
    updateShift.mutate(
      {
        id: shiftId,
        dayOfWeek: Number(dayOfWeek),
        startTime,
        endTime,
        totalSeats: Number(totalSeats),
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
    return (
      <Card className="p-8">
        <div className="flex items-center justify-center gap-2">
          <Spinner size="sm" />
          <span className="text-default-500">{t("common.loading")}</span>
        </div>
      </Card>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Button variant="ghost" className="mb-2" onPress={() => router.push(Routes.Clinic.Appointments.Shifts.Detail(shiftId))}>
          ← {t("common.actions.back")}
        </Button>
        <h1 className="text-2xl font-bold">{t("clinic.appointments.shifts.editTitle")}</h1>
      </div>

      <Card>
        <Card.Content className="flex flex-col gap-4">
          <TextField>
            <Label>Day of Week</Label>
            <Select selectedKey={dayOfWeek} onSelectionChange={(k) => setDayOfWeek(String(k))}>
              <Select.Trigger>
                <Select.Value />
                <Select.Indicator />
              </Select.Trigger>
              <Select.Popover>
                <ListBox>
                  {DAY_NAMES.map((name, i) => (
                    <ListBox.Item key={String(i)} id={String(i)} textValue={name}>
                      {name}
                      <ListBox.ItemIndicator />
                    </ListBox.Item>
                  ))}
                </ListBox>
              </Select.Popover>
            </Select>
          </TextField>

          <div className="grid gap-4 sm:grid-cols-2">
            <TextField>
              <Label>Start Time</Label>
              <Input type="time" value={startTime} onChange={(e) => setStartTime(e.target.value)} />
            </TextField>
            <TextField>
              <Label>End Time</Label>
              <Input type="time" value={endTime} onChange={(e) => setEndTime(e.target.value)} />
            </TextField>
          </div>

          <TextField>
            <Label>Total Seats</Label>
            <Input type="number" value={totalSeats} onChange={(e) => setTotalSeats(e.target.value)} />
          </TextField>

          <div className="flex justify-end gap-2">
            <Button variant="ghost" onPress={() => router.push(Routes.Clinic.Appointments.Shifts.Detail(shiftId))}>
              {t("common.actions.cancel")}
            </Button>
            <Button variant="primary" isPending={updateShift.isPending} onPress={handleSubmit}>
              {t("common.actions.save")}
            </Button>
          </div>
        </Card.Content>
      </Card>
    </div>
  );
}
