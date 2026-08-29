"use client";

import { Button, Calendar, Card, Tooltip } from "@heroui/react";
import {
  getLocalTimeZone,
  today,
  type CalendarDate,
} from "@internationalized/date";
import Image from "next/image";
import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { useClinicListDashboardAppointments } from "@vitarish/clinic";
import { formatDate, formatDateTime, useLocalizedText } from "@/localization";
import { Routes } from "@/app/routes";
import { calendarMonthRange } from "../../calendarMonthRange";

export function AppointmentsWidget() {
  const { t, locale } = useLocalizedText();
  const router = useRouter();
  const [focusedDate, setFocusedDate] = useState<CalendarDate>(() =>
    today(getLocalTimeZone()),
  );
  const [selectedDate, setSelectedDate] = useState<CalendarDate | null>(null);
  const range = useMemo(() => calendarMonthRange(focusedDate), [focusedDate]);
  const query = useClinicListDashboardAppointments(range);
  const rows = query.data ?? [];
  return (
    <Card className="home-dashboard-widget relative flex h-full min-h-[620px] flex-col overflow-hidden p-0">
      <Image
        alt=""
        className="object-cover object-top"
        fill
        sizes="(min-width: 1280px) 33vw, 100vw"
        src="/dashboard-appointments.webp"
      />
      <div className="home-dashboard-widget-content relative z-10 flex h-full flex-col">
        <header className="min-h-40 px-5 py-6 text-white sm:min-h-44 sm:px-6">
          <i
            aria-hidden="true"
            className="fa-light fa-calendar-days text-2xl text-red"
          />
          <h2 className="mt-8 text-2xl font-semibold">
            {t("home.dashboard.appointments")}
          </h2>
          <p className="mt-1 text-sm text-white/80">
            {t("home.dashboard.appointmentsDescription")}
          </p>
        </header>
        <section className="home-dashboard-widget-surface relative z-10 -mt-4 mx-1 flex flex-1 flex-col rounded-3xl bg-background/95 p-4 backdrop-blur-xl sm:p-5">
          <Calendar
            aria-label={t("home.dashboard.appointmentsCalendarLabel")}
            focusedValue={focusedDate}
            value={selectedDate}
            onChange={setSelectedDate}
            onFocusChange={setFocusedDate}
            weeksInMonth={6}
            className="w-full"
          >
            <Calendar.Header>
              <Calendar.Heading />
              <div className="flex gap-1">
                <Calendar.NavButton slot="previous">
                  {t("common.actions.previous")}
                </Calendar.NavButton>
                <Calendar.NavButton slot="next">
                  {t("common.actions.next")}
                </Calendar.NavButton>
              </div>
            </Calendar.Header>
            <Calendar.Grid>
              <Calendar.GridHeader>
                {(day) => <Calendar.HeaderCell>{day}</Calendar.HeaderCell>}
              </Calendar.GridHeader>
              <Calendar.GridBody>
                {(date) => <Calendar.Cell date={date} />}
              </Calendar.GridBody>
            </Calendar.Grid>
          </Calendar>
          <p className="mt-3 text-xs text-muted">
            {selectedDate
              ? formatDate(selectedDate.toDate(getLocalTimeZone()), locale, {
                  dateStyle: "medium",
                })
              : t("home.dashboard.appointmentsCalendarLabel")}
          </p>
          {query.isLoading ? (
            <p className="py-6 text-sm text-muted" aria-live="polite">
              {t("home.dashboard.loadingAppointments")}
            </p>
          ) : query.error ? (
            <div
              className="flex items-center justify-between gap-3 py-6"
              role="alert"
            >
              <span className="text-sm text-danger">
                {t("home.dashboard.appointmentsLoadFailed")}
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
          ) : rows.length === 0 ? (
            <p className="py-6 text-sm text-muted">
              {t("home.dashboard.noAppointments")}
            </p>
          ) : (
            <div className="divide-y divide-separator">
              {rows.map((row) => {
                const fallbackName = row.patientId
                  ? `${t("home.dashboard.patient")} ${row.patientId.slice(0, 8)}`
                  : `${t("home.dashboard.reservation")} ${row.reservationId.slice(0, 8)}`;
                const name = row.patientDisplayName?.trim() || fallbackName;
                return (
                  <div
                    className="flex items-center gap-3 py-3"
                    key={row.reservationId}
                  >
                    <span className="grid size-9 shrink-0 place-items-center rounded-full bg-red-soft text-xs font-semibold text-red-soft-foreground">
                      {name.trim().slice(0, 2).toUpperCase()}
                    </span>
                    <div className="min-w-0 flex-1">
                      <p className="truncate text-sm font-semibold">{name}</p>
                      <p className="truncate text-xs text-muted">
                        {row.serviceDisplayName?.trim() ||
                          t("home.dashboard.reservationStatus", {
                            status: row.status,
                          })}
                      </p>
                    </div>
                    <time className="shrink-0 text-end text-xs font-semibold text-red">
                      {row.startTime
                        ? formatDateTime(
                            `${row.date}T${row.startTime}`,
                            locale,
                            {
                              dateStyle: "medium",
                              timeStyle: "short",
                              timeZone: row.timeZone ?? undefined,
                            },
                          )
                        : formatDate(row.date, locale, { dateStyle: "medium" })}
                    </time>
                  </div>
                );
              })}
            </div>
          )}
          <footer className="mt-auto flex gap-3 border-t border-separator pt-4">
            <Tooltip>
              <Button
                className="min-w-0 flex-1 border-red/60 text-red"
                variant="outline"
                onPress={() =>
                  router.push(Routes.Clinic.Appointments.Reservations.Index)
                }
              >
                <i aria-hidden="true" className="fa-light fa-calendar-days" />
                {t("home.dashboard.viewSchedule")}
              </Button>
              <Tooltip.Content>
                {t("home.dashboard.viewSchedule")}
              </Tooltip.Content>
            </Tooltip>
            <Tooltip>
              <Button
                className="min-w-0 flex-1 bg-red text-red-foreground"
                variant="primary"
                onPress={() =>
                  router.push(Routes.Clinic.Appointments.Reservations.Index)
                }
              >
                <i aria-hidden="true" className="fa-light fa-plus" />
                {t("home.dashboard.addAppointment")}
              </Button>
              <Tooltip.Content>
                {t("home.dashboard.addAppointment")}
              </Tooltip.Content>
            </Tooltip>
          </footer>
        </section>
      </div>
    </Card>
  );
}
