"use client";
import { useState } from "react";
import { Button, Input, Label, TextField } from "@heroui/react";
import { NativeSelect } from "@heroui-pro/react";
import { NumberStepper } from "@heroui-pro/react/number-stepper";
import { useLocalizedText, type MessageKey } from "../../../localization/index";

const dayKeys = [
  "clinic.weekdays.sunday",
  "clinic.weekdays.monday",
  "clinic.weekdays.tuesday",
  "clinic.weekdays.wednesday",
  "clinic.weekdays.thursday",
  "clinic.weekdays.friday",
  "clinic.weekdays.saturday",
] satisfies MessageKey[];
export interface ShiftEditorValue {
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  totalSeats: number;
  timeZone: string;
}
export interface ShiftEditorProps {
  initialValue: ShiftEditorValue;
  isPending: boolean;
  failure?: string;
  onSubmit: (value: ShiftEditorValue) => void;
  onCancel: () => void;
}
export function ShiftEditor({
  initialValue,
  isPending,
  failure,
  onSubmit,
  onCancel,
}: ShiftEditorProps) {
  const { t } = useLocalizedText();
  const [day, setDay] = useState(String(initialValue.dayOfWeek));
  const [start, setStart] = useState(initialValue.startTime);
  const [end, setEnd] = useState(initialValue.endTime);
  const [seats, setSeats] = useState(initialValue.totalSeats);
  return (
    <form
      className="flex flex-col gap-4"
      onSubmit={(event) => {
        event.preventDefault();
        onSubmit({
          dayOfWeek: Number(day),
          startTime: start,
          endTime: end,
          totalSeats: seats,
          timeZone: initialValue.timeZone,
        });
      }}
    >
      <NativeSelect fullWidth>
        <Label>{t("clinic.fields.dayOfWeek")}</Label>
        <NativeSelect.Trigger
          name="dayOfWeek"
          value={day}
          onChange={(event) => setDay(event.target.value)}
        >
          {dayKeys.map((key, index) => (
            <NativeSelect.Option key={key} value={String(index)}>
              {t(key)}
            </NativeSelect.Option>
          ))}
          <NativeSelect.Indicator />
        </NativeSelect.Trigger>
      </NativeSelect>
      <div className="grid gap-4 sm:grid-cols-2">
        <TextField>
          <Label>{t("clinic.fields.startTime")}</Label>
          <Input
            type="time"
            value={start}
            onChange={(event) => setStart(event.target.value)}
          />
        </TextField>
        <TextField>
          <Label>{t("clinic.fields.endTime")}</Label>
          <Input
            type="time"
            value={end}
            onChange={(event) => setEnd(event.target.value)}
          />
        </TextField>
      </div>
      <div className="flex flex-col gap-2">
        <Label>{t("clinic.fields.totalSeats")}</Label>
        <NumberStepper
          aria-label={t("clinic.fields.totalSeats")}
          minValue={1}
          value={seats}
          onChange={setSeats}
        >
          <NumberStepper.Group>
            <NumberStepper.DecrementButton />
            <NumberStepper.Value />
            <NumberStepper.IncrementButton />
          </NumberStepper.Group>
        </NumberStepper>
      </div>
      {failure ? (
        <p role="alert" className="text-sm text-danger">
          {failure}
        </p>
      ) : null}
      <div className="flex justify-end gap-2">
        <Button variant="ghost" onPress={onCancel} isDisabled={isPending}>
          {t("common.actions.cancel")}
        </Button>
        <Button variant="primary" type="submit" isPending={isPending}>
          {t("common.actions.save")}
        </Button>
      </div>
    </form>
  );
}
