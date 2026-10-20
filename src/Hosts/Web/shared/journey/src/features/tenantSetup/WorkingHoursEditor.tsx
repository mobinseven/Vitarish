"use client";

import { Button, Label, ListBox, Select, Surface, TimeField, Tooltip } from "@heroui/react";
import { Time } from "@internationalized/date";
import { useLocalizedText } from "@vitarish/localization";
import { FaIcon } from "@vitarish/ui";
import {
  defaultOpeningPeriod,
  maximumPeriodsPerDay,
  type OpeningPeriodForm,
  type WorkingDayForm,
  type WorkingDayState,
} from "./businessDetailsForm";

const dayStates: WorkingDayState[] = ["unset", "open", "closed"];

function parseTime(value: string): Time | null {
  const match = /^(\d{2}):(\d{2})$/.exec(value);
  return match ? new Time(Number(match[1]), Number(match[2])) : null;
}

function formatTime(value: { hour: number; minute: number } | null): string {
  if (!value) return "";
  return `${String(value.hour).padStart(2, "0")}:${String(value.minute).padStart(2, "0")}`;
}

/** A hint only: the server owns the rule that a period closes after it opens. */
function closesBeforeOpening(period: OpeningPeriodForm): boolean {
  return period.opens !== "" && period.closes !== "" && period.closes <= period.opens;
}

interface WorkingHoursEditorProps {
  days: WorkingDayForm[];
  onChange: (days: WorkingDayForm[]) => void;
}

export function WorkingHoursEditor({ days, onChange }: WorkingHoursEditorProps) {
  const { t } = useLocalizedText();

  const updateDay = (index: number, next: Partial<WorkingDayForm>) =>
    onChange(days.map((day, dayIndex) => (dayIndex === index ? { ...day, ...next } : day)));

  const updatePeriod = (dayIndex: number, periodIndex: number, next: Partial<OpeningPeriodForm>) =>
    updateDay(dayIndex, {
      periods: days[dayIndex].periods.map((period, index) => (index === periodIndex ? { ...period, ...next } : period)),
    });

  return (
    <div className="flex flex-col gap-3">
      {days.map((day, dayIndex) => {
        const dayName = t(`journey.setup.days.${day.day}`);
        return (
          <Surface key={day.day} variant="secondary" className="flex flex-col gap-3 rounded-2xl p-4">
            <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <span className="text-base font-medium text-foreground">{dayName}</span>
              <Select
                aria-label={t("journey.setup.details.dayStateLabel", { day: dayName })}
                className="w-full sm:w-48"
                value={day.state}
                onChange={(value) => updateDay(dayIndex, { state: (value as WorkingDayState | null) ?? "unset" })}
              >
                <Select.Trigger>
                  <Select.Value />
                  <Select.Indicator />
                </Select.Trigger>
                <Select.Popover>
                  <ListBox>
                    {dayStates.map((state) => (
                      <ListBox.Item key={state} id={state} textValue={t(`journey.setup.details.dayState.${state}`)}>
                        {t(`journey.setup.details.dayState.${state}`)}
                        <ListBox.ItemIndicator />
                      </ListBox.Item>
                    ))}
                  </ListBox>
                </Select.Popover>
              </Select>
            </div>
            {day.state === "open" ? (
              <div className="flex flex-col gap-3">
                {day.periods.map((period, periodIndex) => (
                  <div key={periodIndex} className="flex flex-col gap-2">
                    <div className="flex flex-wrap items-end gap-3">
                      <TimeField
                        className="w-32"
                        hourCycle={24}
                        value={parseTime(period.opens)}
                        onChange={(value) => updatePeriod(dayIndex, periodIndex, { opens: formatTime(value) })}
                      >
                        <Label>{t("journey.setup.details.opens")}</Label>
                        {/* Hours come before minutes in both languages, so the field reads left to right. */}
                        <TimeField.Group variant="secondary" dir="ltr">
                          <TimeField.Input>{(segment) => <TimeField.Segment segment={segment} />}</TimeField.Input>
                        </TimeField.Group>
                      </TimeField>
                      <TimeField
                        className="w-32"
                        hourCycle={24}
                        value={parseTime(period.closes)}
                        onChange={(value) => updatePeriod(dayIndex, periodIndex, { closes: formatTime(value) })}
                      >
                        <Label>{t("journey.setup.details.closes")}</Label>
                        {/* Hours come before minutes in both languages, so the field reads left to right. */}
                        <TimeField.Group variant="secondary" dir="ltr">
                          <TimeField.Input>{(segment) => <TimeField.Segment segment={segment} />}</TimeField.Input>
                        </TimeField.Group>
                      </TimeField>
                      {day.periods.length > 1 ? (
                        <Tooltip>
                          <Button
                            isIconOnly
                            variant="tertiary"
                            aria-label={t("journey.setup.details.removePeriod")}
                            onPress={() => updateDay(dayIndex, { periods: day.periods.filter((_, index) => index !== periodIndex) })}
                          >
                            <FaIcon icon="trash-can" aria-hidden="true" />
                          </Button>
                          <Tooltip.Content>{t("journey.setup.details.removePeriod")}</Tooltip.Content>
                        </Tooltip>
                      ) : null}
                    </div>
                    {closesBeforeOpening(period) ? (
                      <p className="text-sm text-danger">{t("journey.setup.details.closesBeforeOpens")}</p>
                    ) : null}
                  </div>
                ))}
                {day.periods.length < maximumPeriodsPerDay ? (
                  <Tooltip>
                    <Button
                      variant="tertiary"
                      className="self-start"
                      onPress={() => updateDay(dayIndex, { periods: [...day.periods, { ...defaultOpeningPeriod }] })}
                    >
                      <FaIcon icon="plus" aria-hidden="true" />
                      {t("journey.setup.details.addPeriod")}
                    </Button>
                    <Tooltip.Content>{t("journey.setup.details.addPeriodHint")}</Tooltip.Content>
                  </Tooltip>
                ) : null}
              </div>
            ) : null}
          </Surface>
        );
      })}
    </div>
  );
}
