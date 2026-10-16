"use client";

import {
  Button,
  Input,
  Label,
  NumberField,
  Surface,
  TextArea,
  TextField,
  Tooltip,
} from "@heroui/react";
import { FaIcon, removeButtonClassName } from "@vitarish/ui";
import { useLocalizedText, type Translate } from "../../localization/index";
import type { SurgeryReminderPolicy } from "../../models/surgeries/surgeryModels";

export interface SurgeryReminderEditorProps {
  reminders: SurgeryReminderPolicy[];
  error?: string;
  onChange: (reminders: SurgeryReminderPolicy[]) => void;
}

function reminderOffsetLabel(minutes: number, t: Translate): string {
  if (!Number.isFinite(minutes) || minutes <= 0) {
    return t("clinic.surgeries.plan.reminderAtStart");
  }
  if (minutes % 1440 === 0) {
    return t("clinic.surgeries.plan.reminderDaysBefore", {
      count: String(minutes / 1440),
    });
  }
  if (minutes % 60 === 0) {
    return t("clinic.surgeries.plan.reminderHoursBefore", {
      count: String(minutes / 60),
    });
  }
  return t("clinic.surgeries.plan.reminderMinutesBefore", {
    count: String(minutes),
  });
}

/** Editable reminder rows; reminders are delivered by the server automatically. */
export function SurgeryReminderEditor({
  reminders,
  error,
  onChange,
}: SurgeryReminderEditorProps) {
  const { t } = useLocalizedText();

  const updateRow = (index: number, patch: Partial<SurgeryReminderPolicy>) => {
    onChange(
      reminders.map((reminder, current) =>
        current === index ? { ...reminder, ...patch } : reminder,
      ),
    );
  };

  const removeRow = (index: number) => {
    onChange(reminders.filter((_, current) => current !== index));
  };

  const addRow = () => {
    onChange([
      ...reminders,
      {
        name: t("clinic.surgeries.plan.defaultReminderName"),
        minutesBefore: 1440,
        subject: "",
        textBody: "",
      },
    ]);
  };

  return (
    <div className="flex flex-col gap-4">
      {reminders.map((reminder, index) => (
        <Surface
          key={index}
          className="flex flex-col gap-4 rounded-2xl p-4"
          variant="secondary"
        >
          <div className="grid gap-3 sm:grid-cols-[minmax(0,12rem)_minmax(0,1fr)] sm:items-end">
            <div className="flex flex-col gap-1.5">
              <NumberField
                aria-label={t("clinic.surgeries.fields.minutesBefore")}
                minValue={0}
                value={Number.isFinite(reminder.minutesBefore) ? reminder.minutesBefore : 0}
                variant="secondary"
                onChange={(value) =>
                  updateRow(index, {
                    minutesBefore: Number.isFinite(value) ? value : 0,
                  })
                }
              >
                <Label>{t("clinic.surgeries.fields.minutesBefore")}</Label>
                <NumberField.Input />
              </NumberField>
              <p className="text-sm text-muted">
                {reminderOffsetLabel(reminder.minutesBefore, t)}
              </p>
            </div>
            <TextField variant="secondary">
              <Label>{t("clinic.surgeries.fields.subject")}</Label>
              <Input
                value={reminder.subject}
                onChange={(event) =>
                  updateRow(index, { subject: event.target.value })
                }
              />
            </TextField>
          </div>
          <TextField variant="secondary">
            <Label>{t("clinic.surgeries.fields.message")}</Label>
            <TextArea
              value={reminder.textBody}
              onChange={(event) =>
                updateRow(index, { textBody: event.target.value })
              }
            />
          </TextField>
          <div className="flex justify-end">
            <Tooltip>
              <Button
                aria-label={t("clinic.surgeries.plan.removeReminder")}
                className={`${removeButtonClassName} text-danger`}
                onPress={() => removeRow(index)}
                variant="ghost"
              >
                <FaIcon aria-hidden="true" icon="xmark" />
                {t("clinic.surgeries.plan.removeReminder")}
              </Button>
              <Tooltip.Content>
                {t("clinic.surgeries.plan.removeReminder")}
              </Tooltip.Content>
            </Tooltip>
          </div>
        </Surface>
      ))}

      <div>
        <Tooltip>
          <Button onPress={addRow} variant="outline">
            <FaIcon aria-hidden="true" icon="bell-plus" />
            {t("clinic.surgeries.plan.addReminder")}
          </Button>
          <Tooltip.Content>{t("clinic.surgeries.plan.addReminder")}</Tooltip.Content>
        </Tooltip>
      </div>

      {error ? (
        <p className="text-sm text-danger" role="alert">
          {error}
        </p>
      ) : null}

      <p className="text-sm text-muted">
        {t("clinic.surgeries.plan.remindersAutomaticNote")}
      </p>
    </div>
  );
}
