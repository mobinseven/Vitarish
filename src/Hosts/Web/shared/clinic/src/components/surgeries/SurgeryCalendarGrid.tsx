"use client";

import { Chip } from "@heroui/react";
import type { CalendarDate } from "@internationalized/date";
import { dateValueToIsoDate, formatDate, formatDateTime, formatNumber, useLocalizedText } from "../../localization";
import type { SurgeryCalendarView } from "../../models/surgeries/surgeryCalendarRange";
import type { Surgery } from "../../models/surgeries/surgeryModels";
import { surgeryStatusChipProps } from "./surgeryStatusChipProps";

export interface SurgeryCalendarGridProps {
  view: SurgeryCalendarView;
  /** The days of the visible range, already ordered from the locale's first day of week. */
  days: CalendarDate[];
  /** The visible surgeries grouped by their clinic-local day key. */
  surgeriesByDay: Map<string, Surgery[]>;
  selectedDay: string | null;
  focusedDate: CalendarDate;
  todayKey: string;
  timeZone: string;
  onSelectDay: (dayKey: string) => void;
}

function chunkIntoWeeks(days: CalendarDate[]): CalendarDate[][] {
  const weeks: CalendarDate[][] = [];
  for (let index = 0; index < days.length; index += 7) {
    weeks.push(days.slice(index, index + 7));
  }
  return weeks;
}

/** Month or week grid of the visible range with per-day surgery markers. */
export function SurgeryCalendarGrid({
  view,
  days,
  surgeriesByDay,
  selectedDay,
  focusedDate,
  todayKey,
  timeZone,
  onSelectDay,
}: SurgeryCalendarGridProps) {
  const { t, locale } = useLocalizedText();
  const weekdayDays = days.slice(0, 7);
  const weeks = chunkIntoWeeks(days);

  return (
    <div className="flex flex-col gap-2">
      <div className="grid grid-cols-7 gap-1 sm:gap-2">
        {weekdayDays.map((day) => (
          <div key={day.toString()} className="px-1 text-center text-sm font-medium text-muted">
            {formatDate(day.toDate(timeZone), locale, { weekday: "short" })}
          </div>
        ))}
      </div>
      <div className="grid grid-cols-7 gap-1 sm:gap-2">
        {weeks.flatMap((week) =>
          week.map((day) => {
            const dayKey = dateValueToIsoDate(day);
            const dayDate = day.toDate(timeZone);
            const surgeries = surgeriesByDay.get(dayKey) ?? [];
            const count = surgeries.length;
            const visibleLines = view === "week" ? surgeries : surgeries.slice(0, 2);
            const hiddenCount = count - visibleLines.length;
            const inFocusedMonth = view === "week" || (day.month === focusedDate.month && day.year === focusedDate.year);
            const isToday = dayKey === todayKey;
            const isSelected = dayKey === selectedDay;

            return (
              <button
                key={dayKey}
                type="button"
                aria-pressed={isSelected}
                aria-label={t("clinic.surgeries.calendar.dayCellLabel", {
                  date: formatDate(dayDate, locale, { dateStyle: "full" }),
                  count: formatNumber(count, locale),
                })}
                className={[
                  "flex min-h-16 flex-col gap-1 rounded-2xl border p-2 text-start transition-colors sm:min-h-24",
                  isSelected
                    ? "border-accent bg-accent-soft"
                    : isToday
                      ? "border-accent bg-surface-secondary"
                      : inFocusedMonth
                        ? "border-border bg-surface-secondary"
                        : "border-transparent bg-surface-secondary opacity-60",
                ]
                  .filter(Boolean)
                  .join(" ")}
                data-testid={`surgery-day-${dayKey}`}
                onClick={() => onSelectDay(dayKey)}
              >
                <span className={`text-sm font-medium ${isToday ? "text-accent" : inFocusedMonth ? "text-foreground" : "text-muted"}`}>
                  {formatDate(dayDate, locale, { day: "numeric" })}
                </span>
                <span className="hidden flex-1 flex-col gap-1 sm:flex">
                  {visibleLines.map((surgery) => (
                    <SurgeryEntryChip key={surgery.id} surgery={surgery} />
                  ))}
                  {hiddenCount > 0 ? (
                    <span className="text-sm text-muted">
                      {t("clinic.surgeries.calendar.moreCount", { count: formatNumber(hiddenCount, locale) })}
                    </span>
                  ) : null}
                </span>
                <span className="text-sm text-muted sm:hidden">
                  {count > 0
                    ? t(
                        count === 1
                          ? "clinic.surgeries.calendar.periodCount.one"
                          : "clinic.surgeries.calendar.periodCount.other",
                        { count: formatNumber(count, locale) },
                      )
                    : null}
                </span>
              </button>
            );
          }),
        )}
      </div>
    </div>
  );
}

/** One surgery in a day cell: its time and patient on a soft chip in the surgery's status color. */
function SurgeryEntryChip({ surgery }: { surgery: Surgery }) {
  const { t, locale } = useLocalizedText();
  const chip = surgeryStatusChipProps(surgery.status);

  return (
    <Chip size="sm" {...chip} className={`${chip.className ?? ""} max-w-full justify-start`}>
      <Chip.Label className="truncate">
        <span className="font-medium">
          {formatDateTime(surgery.scheduledAt, locale, { dateStyle: undefined, timeStyle: "short" })}
        </span>{" "}
        {surgery.patientDisplayName?.trim() || t("clinic.patients.unnamedPatient")}
      </Chip.Label>
    </Chip>
  );
}
