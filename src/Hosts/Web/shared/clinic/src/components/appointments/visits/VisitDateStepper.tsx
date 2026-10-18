"use client";

import { useState } from "react";
import { Button, Calendar, Popover, Tooltip } from "@heroui/react";
import { getLocalTimeZone, type CalendarDate } from "@internationalized/date";
import { FaIcon } from "@vitarish/ui";
import { formatDate, useLocalizedText } from "../../../localization";
import {
  shiftVisitPeriod,
  visitPeriodBounds,
  type VisitPeriod,
} from "../../../models/appointments/visitPeriod";

/**
 * A control whose state narrows what a list shows takes the soft accent while
 * that state is on (docs/hosts/web/color-roles.md).
 */
export const narrowingControlClassName = "bg-accent-soft text-accent-soft-foreground";

export interface VisitDateStepperProps {
  period: VisitPeriod;
  focusedDate: CalendarDate;
  /** Today in the same calendar system as `focusedDate`. */
  today: CalendarDate;
  onFocusedDateChange: (date: CalendarDate) => void;
}

/** Steps the shown day, week, or month back and forth, returns to today, and picks a date. */
export function VisitDateStepper({ period, focusedDate, today, onFocusedDateChange }: VisitDateStepperProps) {
  const { t, locale, direction } = useLocalizedText();
  const timeZone = getLocalTimeZone();
  const [isPickerOpen, setPickerOpen] = useState(false);
  const { start, end } = visitPeriodBounds(period, focusedDate, locale);
  const showsToday = start.compare(today) <= 0 && end.compare(today) >= 0;
  const previousLabel = t(`clinic.appointments.visits.previous.${period}`);
  const nextLabel = t(`clinic.appointments.visits.next.${period}`);
  const periodLabel =
    period === "day"
      ? formatDate(focusedDate.toDate(timeZone), locale, { dateStyle: "full" })
      : period === "month"
        ? formatDate(focusedDate.toDate(timeZone), locale, { month: "long", year: "numeric" })
        : t("clinic.appointments.visits.periodRange", {
            from: formatDate(start.toDate(timeZone), locale, { day: "numeric", month: "short" }),
            to: formatDate(end.toDate(timeZone), locale, { day: "numeric", month: "short", year: "numeric" }),
          });

  return (
    <div
      aria-label={t("clinic.appointments.visits.dateLabel")}
      className="flex min-w-0 flex-wrap items-center justify-center gap-1 sm:justify-start"
      role="group"
    >
      <Tooltip>
        <Button
          aria-label={previousLabel}
          size="sm"
          variant="tertiary"
          onPress={() => onFocusedDateChange(shiftVisitPeriod(focusedDate, period, -1))}
        >
          <FaIcon aria-hidden="true" icon={direction === "rtl" ? "chevron-right" : "chevron-left"} />
          <span className="hidden sm:inline">{t("clinic.appointments.visits.previousShort")}</span>
        </Button>
        <Tooltip.Content>{previousLabel}</Tooltip.Content>
      </Tooltip>

      {/* On a narrow screen the label takes its own line above the buttons. */}
      <span aria-live="polite" className="order-first min-w-0 basis-full px-2 py-1 text-center text-sm font-semibold text-foreground sm:order-none sm:basis-auto sm:py-0">
        {periodLabel}
      </span>

      <Popover isOpen={isPickerOpen} onOpenChange={setPickerOpen}>
        <Tooltip>
          <Button
            isIconOnly
            aria-label={t("clinic.appointments.visits.chooseDate")}
            size="sm"
            variant="tertiary"
          >
            <FaIcon aria-hidden="true" icon="calendar-days" />
          </Button>
          <Tooltip.Content>{t("clinic.appointments.visits.chooseDate")}</Tooltip.Content>
        </Tooltip>
        <Popover.Content className="w-[22rem] max-w-[calc(100vw-2rem)]">
          <Popover.Dialog aria-label={t("clinic.appointments.visits.chooseDate")} className="p-3">
            <Calendar
              aria-label={t("clinic.appointments.visits.dateLabel")}
              className="w-full"
              value={focusedDate}
              onChange={(date) => {
                if (date) onFocusedDateChange(date as CalendarDate);
                setPickerOpen(false);
              }}
            >
              <Calendar.Header>
                <Calendar.NavButton slot="previous" aria-label={t("clinic.appointments.visits.previousMonth")}>
                  <FaIcon aria-hidden="true" icon={direction === "rtl" ? "chevron-right" : "chevron-left"} />
                </Calendar.NavButton>
                <Calendar.YearPickerTrigger aria-label={t("clinic.appointments.visits.selectYear")}>
                  <Calendar.YearPickerTriggerHeading />
                  <Calendar.YearPickerTriggerIndicator />
                </Calendar.YearPickerTrigger>
                <Calendar.NavButton slot="next" aria-label={t("clinic.appointments.visits.nextMonth")}>
                  <FaIcon aria-hidden="true" icon={direction === "rtl" ? "chevron-left" : "chevron-right"} />
                </Calendar.NavButton>
              </Calendar.Header>
              <Calendar.YearPickerGrid aria-label={t("clinic.appointments.visits.selectYear")}>
                <Calendar.YearPickerGridBody>
                  {({ year, formattedYear }) => (
                    <Calendar.YearPickerCell year={year}>{formattedYear}</Calendar.YearPickerCell>
                  )}
                </Calendar.YearPickerGridBody>
              </Calendar.YearPickerGrid>
              <Calendar.Grid weekdayStyle="short">
                <Calendar.GridHeader>
                  {(day) => <Calendar.HeaderCell>{day}</Calendar.HeaderCell>}
                </Calendar.GridHeader>
                <Calendar.GridBody>{(date) => <Calendar.Cell date={date} />}</Calendar.GridBody>
              </Calendar.Grid>
            </Calendar>
          </Popover.Dialog>
        </Popover.Content>
      </Popover>

      <Tooltip>
        <Button
          className={showsToday ? undefined : narrowingControlClassName}
          isDisabled={showsToday}
          size="sm"
          variant="tertiary"
          onPress={() => onFocusedDateChange(today)}
        >
          <FaIcon aria-hidden="true" icon="calendar-day" />
          {t("clinic.appointments.visits.today")}
        </Button>
        <Tooltip.Content>{t("clinic.appointments.visits.backToToday")}</Tooltip.Content>
      </Tooltip>

      <Tooltip>
        <Button
          aria-label={nextLabel}
          size="sm"
          variant="tertiary"
          onPress={() => onFocusedDateChange(shiftVisitPeriod(focusedDate, period, 1))}
        >
          <span className="hidden sm:inline">{t("clinic.appointments.visits.nextShort")}</span>
          <FaIcon aria-hidden="true" icon={direction === "rtl" ? "chevron-left" : "chevron-right"} />
        </Button>
        <Tooltip.Content>{nextLabel}</Tooltip.Content>
      </Tooltip>
    </div>
  );
}
