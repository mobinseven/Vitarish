"use client";

import { Button, Calendar, Card, Surface, Tooltip } from "@heroui/react";
import {
  createCalendar,
  getLocalTimeZone,
  toCalendar,
  today,
  type CalendarDate,
} from "@internationalized/date";
import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import {
  surgeryCalendarRange,
  surgeryDayKey,
  surgeryStatuses,
  SurgeryList,
  useCanPlanSurgeries,
  useSurgeryCalendar,
  type Surgery,
} from "@vitarish/clinic";
import { dateValueToIsoDate, formatDate, getDateCalendar, useLocalizedText } from "@/localization";
import { Routes } from "@/app/routes";
import styles from "../HomeWidgets.module.css";

const maxMonthRows = 6;

function sortByStart(surgeries: Surgery[]): Surgery[] {
  return [...surgeries].sort(
    (a, b) =>
      new Date(a.scheduledAt).getTime() - new Date(b.scheduledAt).getTime(),
  );
}

export function SurgeriesWidget() {
  const { t, locale } = useLocalizedText();
  const canPlan = useCanPlanSurgeries();
  const router = useRouter();
  const timeZone = getLocalTimeZone();
  const [focusedDate, setFocusedDate] = useState<CalendarDate>(() =>
    toCalendar(today(timeZone), createCalendar(getDateCalendar(locale))),
  );
  const [selectedDate, setSelectedDate] = useState<CalendarDate | null>(null);

  const range = useMemo(
    () => surgeryCalendarRange("month", focusedDate, timeZone, locale),
    [focusedDate, timeZone, locale],
  );
  const query = useSurgeryCalendar({ from: range.from, to: range.to });
  const surgeries = query.data ?? [];

  const surgeriesByDay = useMemo(() => {
    const counts = new Map<string, number>();
    for (const surgery of surgeries) {
      if (surgery.status !== surgeryStatuses.planned) continue;
      const dayKey = surgeryDayKey(surgery.scheduledAt, timeZone);
      counts.set(dayKey, (counts.get(dayKey) ?? 0) + 1);
    }
    return counts;
  }, [surgeries, timeZone]);

  const selectedDayKey = selectedDate
    ? dateValueToIsoDate(selectedDate)
    : undefined;
  const visibleSurgeries = useMemo(() => {
    if (selectedDayKey) {
      return sortByStart(
        surgeries.filter(
          (surgery) =>
            surgeryDayKey(surgery.scheduledAt, timeZone) === selectedDayKey,
        ),
      );
    }
    const now = Date.now();
    return sortByStart(
      surgeries.filter(
        (surgery) =>
          surgery.status === surgeryStatuses.planned &&
          new Date(surgery.scheduledAt).getTime() >= now,
      ),
    );
  }, [surgeries, selectedDayKey, timeZone]);

  const monthRows = selectedDayKey
    ? visibleSurgeries
    : visibleSurgeries.slice(0, maxMonthRows);
  const hiddenCount = visibleSurgeries.length - monthRows.length;

  const toggleSelectedDate = (date: CalendarDate | null) => {
    if (!date) {
      setSelectedDate(null);
      return;
    }
    setSelectedDate((current) =>
      current && current.compare(date) === 0 ? null : date,
    );
  };

  return (
    <Card className="relative flex h-full min-h-[620px] flex-col overflow-hidden bg-anchor p-0">
      <div className={`${styles.content} relative z-10 flex h-full flex-col`}>
        <header className="min-h-40 px-5 py-6 text-anchor-foreground sm:min-h-44 sm:px-6">
          <i
            aria-hidden="true"
            className="fa-light fa-scissors text-2xl text-foreground"
          />
          <h2 className="mt-8 text-2xl font-semibold">
            {t("home.dashboard.surgeries")}
          </h2>
          <p className="mt-1 text-sm text-anchor-foreground/75">
            {t("home.dashboard.surgeriesDescription")}
          </p>
        </header>
        <Surface className={`${styles.surface} relative z-10 -mt-4 mx-1 flex flex-1 flex-col rounded-3xl p-4 backdrop-blur-xl sm:p-5`}>
            <Calendar
              aria-label={t("home.dashboard.surgeriesCalendarLabel")}
              focusedValue={focusedDate}
              value={selectedDate}
              onChange={toggleSelectedDate}
              onFocusChange={setFocusedDate}
              weeksInMonth={6}
              className="w-full"
            >
              <Calendar.Header>
                <Calendar.Heading />
                <div className="flex gap-1">
                  <Calendar.NavButton
                    className="h-6 w-auto min-w-fit px-1.5 text-sm"
                    slot="previous"
                  >
                    {t("common.actions.previous")}
                  </Calendar.NavButton>
                  <Calendar.NavButton
                    className="h-6 w-auto min-w-fit px-1.5 text-sm"
                    slot="next"
                  >
                    {t("common.actions.next")}
                  </Calendar.NavButton>
                </div>
              </Calendar.Header>
              <Calendar.Grid>
                <Calendar.GridHeader>
                  {(day) => <Calendar.HeaderCell>{day}</Calendar.HeaderCell>}
                </Calendar.GridHeader>
                <Calendar.GridBody>
                  {(date) => {
                    const count =
                      surgeriesByDay.get(dateValueToIsoDate(date)) ?? 0;
                    return (
                      <Calendar.Cell date={date}>
                        <span className="flex h-full w-full flex-col items-center justify-center gap-0.5">
                          <span>
                            {formatDate(date.toDate(timeZone), locale, {
                              day: "numeric",
                            })}
                          </span>
                          {count > 0 ? (
                            <span className="flex h-3 items-center justify-center">
                              <span
                                aria-hidden="true"
                                className={
                                  count === 1
                                    ? "block size-1.5 rounded-full bg-success"
                                    : "min-w-4 rounded-full bg-success/15 px-1 text-center text-xs font-semibold leading-4 text-success"
                                }
                              >
                                {count > 1 ? count : null}
                              </span>
                              <span className="sr-only">
                                {count === 1
                                  ? t("home.dashboard.oneSurgeryOnDay")
                                  : t("home.dashboard.surgeriesOnDay", {
                                      count,
                                    })}
                              </span>
                            </span>
                          ) : null}
                        </span>
                      </Calendar.Cell>
                    );
                  }}
                </Calendar.GridBody>
              </Calendar.Grid>
            </Calendar>
            <div className="mt-3 flex items-center justify-between gap-2">
              <p className="text-sm text-muted">
                {selectedDate
                  ? formatDate(selectedDate.toDate(timeZone), locale, {
                      dateStyle: "medium",
                    })
                  : t("home.dashboard.surgeriesCalendarLabel")}
              </p>
              {selectedDate ? (
                <Tooltip>
                  <Button
                    size="sm"
                    variant="tertiary"
                    onPress={() => setSelectedDate(null)}
                  >
                    <i aria-hidden="true" className="fa-light fa-calendar-days" />
                    {t("home.dashboard.showWholeMonth")}
                  </Button>
                  <Tooltip.Content>
                    {t("home.dashboard.showWholeMonth")}
                  </Tooltip.Content>
                </Tooltip>
              ) : null}
            </div>
            {query.isLoading ? (
              <p className="py-6 text-sm text-muted" aria-live="polite">
                {t("home.dashboard.loadingSurgeries")}
              </p>
            ) : query.error ? (
              <div
                className="flex items-center justify-between gap-3 py-6"
                role="alert"
              >
                <span className="text-sm text-danger">
                  {t("home.dashboard.surgeriesLoadFailed")}
                </span>
                <Tooltip>
                  <Button
                    size="sm"
                    variant="outline"
                    onPress={() => query.refetch()}
                  >
                    <i aria-hidden="true" className="fa-light fa-rotate-right" />
                    {t("home.dashboard.retry")}
                  </Button>
                  <Tooltip.Content>{t("home.dashboard.retry")}</Tooltip.Content>
                </Tooltip>
              </div>
            ) : (
              <div className="flex flex-col">
                <SurgeryList
                  emptyMessage={
                    selectedDayKey
                      ? t("home.dashboard.noSurgeriesForDay")
                      : t("home.dashboard.noSurgeries")
                  }
                  onOpen={(surgery) =>
                    router.push(Routes.Clinic.Surgeries.Detail(surgery.id))
                  }
                  showDate={!selectedDayKey}
                  surgeries={monthRows}
                />
                {hiddenCount > 0 ? (
                  <p className="pt-2 text-sm text-muted">
                    {t("home.dashboard.moreSurgeries", { count: hiddenCount })}
                  </p>
                ) : null}
              </div>
            )}
          <footer className="mt-auto flex gap-3 border-t border-separator pt-4">
            <Tooltip>
              <Button
                className="min-w-0 flex-1"
                variant="outline"
                onPress={() => router.push(Routes.Clinic.Surgeries.Index)}
              >
                <i aria-hidden="true" className="fa-light fa-calendar-days" />
                {t("home.dashboard.viewCalendar")}
              </Button>
              <Tooltip.Content>
                {t("home.dashboard.viewCalendar")}
              </Tooltip.Content>
            </Tooltip>
            {canPlan ? (
              <Tooltip>
                <Button
                  className="min-w-0 flex-1"
                  variant="primary"
                  onPress={() => router.push(Routes.Clinic.Surgeries.New)}
                >
                  <i aria-hidden="true" className="fa-light fa-plus" />
                  {t("home.dashboard.addSurgery")}
                </Button>
                <Tooltip.Content>
                  {t("home.dashboard.addSurgery")}
                </Tooltip.Content>
              </Tooltip>
            ) : null}
          </footer>
        </Surface>
      </div>
    </Card>
  );
}
