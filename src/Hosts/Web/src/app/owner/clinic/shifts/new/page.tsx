"use client";

import { useCallback, useState } from "react";
import { useRouter } from "next/navigation";
import { Card, Button, TextField, Label, Input, Select, ListBox } from "@heroui/react";
import { Routes } from "@/app/routes";
import { useCreateShift } from "@/features/clinic/appointments/shifts/hooks/useOwnerShifts";
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

export default function ClinicOwnerShiftCreatePage() {
  const router = useRouter();
  const { t } = useLocalizedText();
  const createShift = useCreateShift();

  const [dayOfWeek, setDayOfWeek] = useState("1");
  const [startTime, setStartTime] = useState("09:00");
  const [endTime, setEndTime] = useState("10:00");
  const [totalSeats, setTotalSeats] = useState("10");

  const handleSubmit = useCallback(() => {
    createShift.mutate(
      {
        dayOfWeek: Number(dayOfWeek),
        startTime,
        endTime,
        totalSeats: Number(totalSeats),
        timeZone: "UTC",
      },
      {
        onSuccess: (result) => {
          if (result.succeeded && result.entityId) {
            router.push(Routes.Clinic.Appointments.Shifts.Detail(result.entityId));
          }
        },
      }
    );
  }, [dayOfWeek, startTime, endTime, totalSeats, createShift, router]);

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Button variant="ghost" className="mb-2" onPress={() => router.push(Routes.Clinic.Appointments.Shifts.Index)}>
          ← {t("common.actions.previous")}
        </Button>
        <h1 className="text-2xl font-bold">{t("clinic.appointments.shifts.createTitle")}</h1>
      </div>

      <Card>
        <Card.Content className="flex flex-col gap-4">
          <TextField>
            <Label>{t("clinic.fields.dayOfWeek")}</Label>
            <Select selectedKey={dayOfWeek} onSelectionChange={(k) => setDayOfWeek(String(k))}>
              <Select.Trigger>
                <Select.Value />
                <Select.Indicator />
              </Select.Trigger>
              <Select.Popover>
                <ListBox>
                  {dayNameKeys.map((key, i) => {
                    const name = t(key);
                    return (
                      <ListBox.Item key={String(i)} id={String(i)} textValue={name}>
                        {name}
                        <ListBox.ItemIndicator />
                      </ListBox.Item>
                    );
                  })}
                </ListBox>
              </Select.Popover>
            </Select>
          </TextField>

          <div className="grid gap-4 sm:grid-cols-2">
            <TextField>
              <Label>{t("clinic.fields.startTime")}</Label>
              <Input type="time" value={startTime} onChange={(e) => setStartTime(e.target.value)} />
            </TextField>
            <TextField>
              <Label>{t("clinic.fields.endTime")}</Label>
              <Input type="time" value={endTime} onChange={(e) => setEndTime(e.target.value)} />
            </TextField>
          </div>

          <TextField>
            <Label>{t("clinic.fields.totalSeats")}</Label>
            <Input type="number" value={totalSeats} onChange={(e) => setTotalSeats(e.target.value)} />
          </TextField>

          <div className="flex justify-end gap-2">
            <Button variant="ghost" onPress={() => router.push(Routes.Clinic.Appointments.Shifts.Index)}>
              {t("common.actions.cancel")}
            </Button>
            <Button variant="primary" isPending={createShift.isPending} onPress={handleSubmit}>
              {t("common.actions.save")}
            </Button>
          </div>
        </Card.Content>
      </Card>
    </div>
  );
}
