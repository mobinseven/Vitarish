"use client";

import { Button, Card, Chip, InputGroup, TextField, Tooltip } from "@heroui/react";
import {
  getLocalTimeZone,
  today,
  type CalendarDate,
} from "@internationalized/date";
import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { useClinicListDashboardAppointments } from "@vitarish/clinic";
import { formatDate, formatNumber, useLocalizedText } from "@/localization";
import { Routes } from "@/app/routes";
import { calendarMonthRange } from "../calendarMonthRange";
import styles from "./HomeDashboard.module.css";

type SortMode = "time" | "patient";
type StatusColor = "accent" | "default" | "danger" | "success" | "warning";

const patientIdPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export function HomeDashboard() {
  const { t, locale } = useLocalizedText();
  const router = useRouter();
  const [focusedDate, setFocusedDate] = useState<CalendarDate>(() =>
    today(getLocalTimeZone()),
  );
  const [search, setSearch] = useState("");
  const [sortMode, setSortMode] = useState<SortMode>("time");
  const range = useMemo(() => calendarMonthRange(focusedDate), [focusedDate]);
  const query = useClinicListDashboardAppointments(range);
  const rows = query.data ?? [];
  const visibleRows = useMemo(() => {
    const queryText = search.trim().toLocaleLowerCase(locale);
    return rows
      .filter((row) => {
        if (!queryText) return true;
        return [row.patientDisplayName, row.serviceDisplayName, row.reservationId]
          .filter(Boolean)
          .some((value) => value?.toLocaleLowerCase(locale).includes(queryText));
      })
      .sort((left, right) => {
        if (sortMode === "patient") {
          return getPatientLabel(left.patientDisplayName, t)
            .localeCompare(getPatientLabel(right.patientDisplayName, t), locale);
        }

        return getAppointmentSortValue(left) - getAppointmentSortValue(right);
      });
  }, [locale, rows, search, sortMode, t]);

  function moveMonth(months: number) {
    setFocusedDate((current) => current.add({ months }));
  }

  function openReservation(reservationId: string) {
    router.push(Routes.Clinic.Appointments.Reservations.Detail(reservationId));
  }

  function openPatient(patientId: string | null) {
    if (patientId && patientIdPattern.test(patientId)) {
      router.push(Routes.Clinic.Patients.Detail(patientId));
      return;
    }

    router.push(Routes.Clinic.Patients.Index);
  }

  const monthLabel = formatDate(focusedDate.toDate(getLocalTimeZone()), locale, {
    dateStyle: "long",
  });

  return (
    <div className={`${styles.root} relative flex min-h-full flex-col gap-5 overflow-hidden px-4 py-5 sm:px-6 lg:px-8 lg:py-6`}>
      <header className="relative z-10 flex flex-col gap-2 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <p className="mb-2 flex items-center gap-2 text-sm font-semibold text-accent">
            <i aria-hidden="true" className="fa-light fa-calendar-check" />
            {t("home.dashboard.eyebrow")}
          </p>
          <h1 className="text-3xl font-semibold tracking-tight text-foreground sm:text-4xl">
            {t("home.dashboard.reviewBrowserTitle")}
          </h1>
          <p className="mt-2 max-w-2xl text-base text-muted">
            {t("home.dashboard.reviewBrowserDescription")}
          </p>
        </div>
        <div className="flex items-center gap-2 text-sm text-muted">
          <span className="size-2 rounded-full bg-success" aria-hidden="true" />
          {t("home.dashboard.liveSchedule")}
        </div>
      </header>

      <Card className={`${styles.panel} relative z-10 overflow-hidden p-0`}>
        <Card.Header className={`${styles.header} gap-4 border-b border-separator/70 px-4 py-4 sm:px-6 lg:px-8`}>
          <div className="flex flex-col gap-4 xl:flex-row xl:items-center xl:justify-between">
            <div className="flex items-center gap-3">
              <div className={`${styles.headerIcon} grid size-10 shrink-0 place-items-center rounded-xl`}>
                <i aria-hidden="true" className="fa-light fa-calendar-days text-lg" />
              </div>
              <div>
                <Card.Title>{t("home.dashboard.upcomingVisits")}</Card.Title>
                <Card.Description>
                  {t("home.dashboard.resultsCount", {
                    count: formatNumber(visibleRows.length, locale),
                  })}
                </Card.Description>
              </div>
            </div>

            <div className="flex flex-wrap items-center gap-2" aria-label={t("home.dashboard.controlsLabel")}>
              <Tooltip>
                <Button
                  className="min-w-0"
                  size="sm"
                  variant="outline"
                  onPress={() => setSortMode((current) => (current === "time" ? "patient" : "time"))}
                >
                  <i aria-hidden="true" className="fa-light fa-arrow-down-a-z" />
                  <span className="hidden sm:inline">
                    {sortMode === "time"
                      ? t("home.dashboard.sortByTime")
                      : t("home.dashboard.sortByPatient")}
                  </span>
                </Button>
                <Tooltip.Content>
                  {sortMode === "time"
                    ? t("home.dashboard.sortByTime")
                    : t("home.dashboard.sortByPatient")}
                </Tooltip.Content>
              </Tooltip>
              <Tooltip>
                <Button
                  className="min-w-0"
                  size="sm"
                  variant="outline"
                  onPress={() => setSearch("")}
                >
                  <i aria-hidden="true" className="fa-light fa-filter" />
                  <span className="hidden sm:inline">{t("home.dashboard.clearFilters")}</span>
                </Button>
                <Tooltip.Content>{t("home.dashboard.clearFilters")}</Tooltip.Content>
              </Tooltip>
            </div>
          </div>

          <div className="grid gap-3 lg:grid-cols-[minmax(0,1fr)_auto]">
            <TextField aria-label={t("home.dashboard.searchLabel")} className="min-w-0">
              <InputGroup>
                <InputGroup.Prefix>
                  <i aria-hidden="true" className="fa-light fa-magnifying-glass text-muted" />
                </InputGroup.Prefix>
                <InputGroup.Input
                  placeholder={t("home.dashboard.searchPlaceholder")}
                  value={search}
                  onChange={(event) => setSearch(event.target.value)}
                />
              </InputGroup>
            </TextField>
            <div className="flex items-center justify-between gap-2 rounded-2xl bg-surface-secondary px-3 py-2 lg:min-w-72">
              <Tooltip>
                <Button
                  isIconOnly
                  aria-label={t("common.actions.previous")}
                  size="sm"
                  variant="ghost"
                  onPress={() => moveMonth(-1)}
                >
                  <i aria-hidden="true" className="fa-light fa-calendar-minus" />
                </Button>
                <Tooltip.Content>{t("common.actions.previous")}</Tooltip.Content>
              </Tooltip>
              <span className="numeric min-w-0 truncate text-center text-sm font-semibold text-foreground">
                {monthLabel}
              </span>
              <Tooltip>
                <Button
                  isIconOnly
                  aria-label={t("common.actions.next")}
                  size="sm"
                  variant="ghost"
                  onPress={() => moveMonth(1)}
                >
                  <i aria-hidden="true" className="fa-light fa-calendar-plus" />
                </Button>
                <Tooltip.Content>{t("common.actions.next")}</Tooltip.Content>
              </Tooltip>
            </div>
          </div>
        </Card.Header>

        <Card.Content className="p-0">
          {query.isLoading ? (
            <p className="px-6 py-16 text-center text-sm text-muted" aria-live="polite">
              {t("home.dashboard.loadingAppointments")}
            </p>
          ) : query.error ? (
            <div className="flex flex-col items-center gap-3 px-6 py-16 text-center" role="alert">
              <i aria-hidden="true" className="fa-light fa-triangle-exclamation text-2xl text-danger" />
              <p className="text-sm text-danger">{t("home.dashboard.appointmentsLoadFailed")}</p>
              <Tooltip>
                <Button size="sm" variant="outline" onPress={() => void query.refetch()}>
                  <i aria-hidden="true" className="fa-light fa-rotate-right" />
                  {t("home.dashboard.retry")}
                </Button>
                <Tooltip.Content>{t("home.dashboard.retry")}</Tooltip.Content>
              </Tooltip>
            </div>
          ) : visibleRows.length === 0 ? (
            <div className="flex flex-col items-center gap-3 px-6 py-16 text-center">
              <i aria-hidden="true" className="fa-light fa-calendar-xmark text-2xl text-muted" />
              <p className="text-sm text-muted">
                {search ? t("home.dashboard.noSearchResults") : t("home.dashboard.noAppointments")}
              </p>
            </div>
          ) : (
            <div className={`${styles.table} overflow-x-auto`}>
              <div className="min-w-[900px]">
                <div className={`${styles.tableHeader} ${styles.gridColumns} grid gap-4 border-b border-separator px-4 py-3 text-xs font-semibold sm:px-6 lg:px-8`}>
                  <span>{t("home.dashboard.patientColumn")}</span>
                  <span>{t("home.dashboard.timeColumn")}</span>
                  <span>{t("home.dashboard.serviceColumn")}</span>
                  <span>{t("home.dashboard.statusColumn")}</span>
                  <span>{t("home.dashboard.actionsColumn")}</span>
                </div>
                <div className="divide-y divide-separator/70">
                  {visibleRows.map((row) => {
                    const patientLabel = getPatientLabel(row.patientDisplayName, t);
                    const status = getStatusMeta(row.status, t);
                    const timeLabel = row.startTime
                      ? formatDate(`${row.date}T${row.startTime}`, locale, {
                          timeStyle: "short",
                          timeZone: row.timeZone ?? undefined,
                        })
                      : t("home.dashboard.timeUnavailable");

                    return (
                      <div
                        className={`${styles.row} ${styles.gridColumns} grid items-center gap-4 px-4 py-3 transition-colors sm:px-6 lg:px-8`}
                        key={row.reservationId}
                      >
                        <div className="flex min-w-0 items-center gap-3">
                          <span className={`${styles.avatar} grid size-10 shrink-0 place-items-center rounded-full text-sm font-semibold`}>
                            {getInitials(patientLabel)}
                          </span>
                          <div className="min-w-0">
                            <p className="truncate text-sm font-semibold text-foreground">{patientLabel}</p>
                            <p className="truncate text-xs text-muted">
                              {row.patientId
                                ? t("home.dashboard.patientRecord")
                                : t("home.dashboard.patientIdUnavailable")}
                            </p>
                          </div>
                        </div>
                        <div>
                          <time className={`${styles.numeric} block text-sm font-semibold text-foreground`}>{timeLabel}</time>
                        </div>
                        <div className="min-w-0">
                          <p className="truncate text-sm font-medium text-foreground">
                            {getServiceLabel(row.serviceDisplayName, locale, t)}
                          </p>
                        </div>
                        <Chip className={`${styles.statusChip} w-fit`} color={status.color} size="sm" variant="soft">
                          <span className="size-1.5 rounded-full bg-current" aria-hidden="true" />
                          <Chip.Label>{status.label}</Chip.Label>
                        </Chip>
                        <div className={styles.actions}>
                          <Tooltip>
                            <Button className={styles.primary} size="sm" variant="primary" onPress={() => openPatient(row.patientId)}>
                              <i aria-hidden="true" className="fa-light fa-user" />
                              {t("home.dashboard.viewPatient")}
                            </Button>
                            <Tooltip.Content>{t("home.dashboard.viewPatient")}</Tooltip.Content>
                          </Tooltip>
                          <Tooltip>
                            <Button size="sm" variant="outline" onPress={() => openReservation(row.reservationId)}>
                              <i aria-hidden="true" className="fa-light fa-folder-open" />
                              {t("home.dashboard.viewFile")}
                            </Button>
                            <Tooltip.Content>{t("home.dashboard.viewFile")}</Tooltip.Content>
                          </Tooltip>
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>
            </div>
          )}
        </Card.Content>
      </Card>
    </div>
  );
}

function getAppointmentSortValue(row: { date: string; startTime: string | null }) {
  return Date.parse(`${row.date}T${row.startTime ?? "00:00:00"}`);
}

function getPatientLabel(
  patientDisplayName: string | null,
  t: (key: string) => string,
) {
  return patientDisplayName?.trim() || t("home.dashboard.patientUnavailable");
}

function getServiceLabel(
  serviceDisplayName: string | null,
  locale: string,
  t: (key: string) => string,
) {
  const service = serviceDisplayName?.trim();
  if (!service) return t("home.dashboard.serviceUnavailable");
  if (locale.startsWith("fa")) {
    if (service === "Initial consultation") return t("home.dashboard.initialConsultation");
    if (service === "Follow-up visit") return t("home.dashboard.followUpVisit");
  }
  return service;
}

function getInitials(label: string) {
  const initials = label
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0])
    .join("");

  return initials || "—";
}

function getStatusMeta(
  status: number,
  t: (key: string) => string,
): { color: StatusColor; label: string } {
  switch (status) {
    case 0:
      return { color: "warning", label: t("clinic.reservationStatus.pending") };
    case 1:
      return { color: "success", label: t("clinic.reservationStatus.confirmed") };
    case 2:
    case 3:
      return { color: "danger", label: t("clinic.reservationStatus.cancelled") };
    case 4:
      return { color: "warning", label: t("clinic.reservationStatus.expired") };
    case 5:
      return { color: "accent", label: t("clinic.reservationStatus.waitlisted") };
    case 6:
      return { color: "success", label: t("clinic.reservationStatus.present") };
    case 7:
      return { color: "success", label: t("clinic.reservationStatus.completed") };
    default:
      return { color: "default", label: t("home.dashboard.unknownStatus") };
  }
}
