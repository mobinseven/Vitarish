"use client";

import {
  Button,
  Calendar,
  Card,
  Chip,
  DateField,
  DatePicker,
  InputGroup,
  Label,
  ListBox,
  Popover,
  Select,
  TextField,
  Tooltip,
} from "@heroui/react";
import {
  getLocalTimeZone,
  today,
  type CalendarDate,
} from "@internationalized/date";
import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { useClinicListDashboardAppointments } from "@vitarish/clinic";
import { formatNumber, useLocalizedText } from "@/localization";
import { Routes } from "@/app/routes";
import { calendarDayRange } from "../calendarDayRange";
import styles from "./HomeDashboard.module.css";
import {
  HeaderIcon,
  actionRoleClassName,
  contextColorClassName,
  identityColorClassName,
  identitySlotForId,
} from "@vitarish/ui";

type SortMode = "time" | "patient";
type StatusColor = "default" | "danger" | "success" | "warning";
type FilterOption = { value: string; label: string };

const patientIdPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export function HomeDashboard() {
  const { direction, t, locale } = useLocalizedText();
  const router = useRouter();
  const [focusedDate, setFocusedDate] = useState<CalendarDate>(() =>
    today(getLocalTimeZone()),
  );
  const [search, setSearch] = useState("");
  const [sortMode, setSortMode] = useState<SortMode>("time");
  const [selectedStatuses, setSelectedStatuses] = useState<string[]>([]);
  const [selectedServiceIds, setSelectedServiceIds] = useState<string[]>([]);
  const range = useMemo(() => calendarDayRange(focusedDate), [focusedDate]);
  const query = useClinicListDashboardAppointments(range);
  const rows = query.data ?? [];
  const statusOptions = useMemo<FilterOption[]>(() => {
    return [...new Set(rows.map((row) => row.status))]
      .sort((left, right) => left - right)
      .map((status) => ({
        value: String(status),
        label: getStatusMeta(status, t).label,
      }));
  }, [rows, t]);
  const serviceOptions = useMemo<FilterOption[]>(() => {
    const options = new Map<string, string>();
    for (const row of rows) {
      const value = getServiceFilterKey(row.serviceId, row.serviceDisplayName, locale);
      options.set(value, getServiceLabel(row.serviceDisplayName, locale, t));
    }

    return [...options]
      .map(([value, label]) => ({ value, label }))
      .sort((left, right) => left.label.localeCompare(right.label, locale));
  }, [locale, rows, t]);
  const activeFilterCount =
    selectedStatuses.length +
    selectedServiceIds.length +
    (search.trim() ? 1 : 0);

  useEffect(() => {
    setSelectedStatuses([]);
    setSelectedServiceIds([]);
  }, [focusedDate]);

  const visibleRows = useMemo(() => {
    const queryText = search.trim().toLocaleLowerCase(locale);
    const statusSet = new Set(selectedStatuses);
    const serviceSet = new Set(selectedServiceIds);

    return rows
      .filter((row) => {
        if (statusSet.size > 0 && !statusSet.has(String(row.status))) return false;
        if (
          serviceSet.size > 0 &&
          !serviceSet.has(getServiceFilterKey(row.serviceId, row.serviceDisplayName, locale))
        ) {
          return false;
        }
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
  }, [locale, rows, search, selectedServiceIds, selectedStatuses, sortMode, t]);

  function moveDay(days: number) {
    setFocusedDate((current) => current.add({ days }));
  }

  function clearFilters() {
    setSearch("");
    setSelectedStatuses([]);
    setSelectedServiceIds([]);
  }

  function openPatient(patientId: string | null) {
    if (patientId && patientIdPattern.test(patientId)) {
      router.push(Routes.Clinic.Patients.Detail(patientId));
      return;
    }

    router.push(Routes.Clinic.Patients.Index);
  }

  return (
    <div className={`${styles.root} relative flex min-h-full flex-col gap-5 overflow-hidden px-4 py-5 sm:px-6 lg:px-8 lg:py-6`}>
      <header className="relative z-10 flex flex-col gap-2 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <div className="flex items-center gap-4">
            <HeaderIcon context="general" icon="house" level="page" />
            <div className="min-w-0">
              <h1 className="text-3xl font-semibold tracking-tight text-foreground sm:text-4xl">
                {t("home.dashboard.reviewBrowserTitle")}
              </h1>
            </div>
          </div>
        </div>
      </header>

      <Card className={`${styles.panel} relative z-10 overflow-hidden p-0`}>
        <Card.Header className={`${styles.header} gap-4 border-b border-separator/70 px-4 py-4 sm:px-6 lg:px-8`}>
          <div className="flex flex-col gap-4 xl:flex-row xl:items-center xl:justify-between">
            <div className="flex items-center gap-3">
              <div className={`${contextColorClassName("scheduling")} ${styles.headerIcon} grid size-10 shrink-0 place-items-center rounded-xl`}>
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
              <Popover>
                <Button
                  aria-label={t("home.dashboard.filters")}
                  className="min-w-0"
                  size="sm"
                  variant={activeFilterCount > 0 ? "secondary" : "outline"}
                >
                  <i aria-hidden="true" className="fa-light fa-filter" title={t("home.dashboard.filters")} />
                  <span className="hidden sm:inline">{t("home.dashboard.filters")}</span>
                  {activeFilterCount > 0 ? (
                    <Chip
                      aria-label={t("home.dashboard.activeFilterCount", {
                        count: formatNumber(activeFilterCount, locale),
                      })}
                      color="default"
                      size="sm"
                      variant="soft"
                    >
                      <Chip.Label>{formatNumber(activeFilterCount, locale)}</Chip.Label>
                    </Chip>
                  ) : null}
                </Button>
                <Popover.Content className="w-[22rem] max-w-[calc(100vw-2rem)]">
                  <Popover.Dialog className="space-y-4 p-4">
                    <div className="flex items-center justify-between gap-3">
                      <Popover.Heading className="font-semibold">
                        {t("home.dashboard.filterHeading")}
                      </Popover.Heading>
                      {activeFilterCount > 0 ? (
                        <Button size="sm" variant="ghost" onPress={clearFilters}>
                          <i aria-hidden="true" className="fa-light fa-rotate-left" />
                          {t("home.dashboard.clearFilters")}
                        </Button>
                      ) : null}
                    </div>

                    <div className="space-y-3">
                      <Select
                        aria-label={t("home.dashboard.statusFilterLabel")}
                        className="w-full"
                        isDisabled={statusOptions.length === 0}
                        placeholder={t("home.dashboard.allStatuses")}
                        selectionMode="multiple"
                        value={selectedStatuses}
                        onChange={(value) => setSelectedStatuses(toSelectionValues(value))}
                      >
                        <Label>{t("home.dashboard.statusFilterLabel")}</Label>
                        <Select.Trigger>
                          <Select.Value />
                          <Select.Indicator />
                        </Select.Trigger>
                        <Select.Popover>
                          <ListBox>
                            {statusOptions.map((option) => (
                              <ListBox.Item key={option.value} id={option.value} textValue={option.label}>
                                {option.label}
                                <ListBox.ItemIndicator />
                              </ListBox.Item>
                            ))}
                          </ListBox>
                        </Select.Popover>
                      </Select>

                      <Select
                        aria-label={t("home.dashboard.serviceFilterLabel")}
                        className="w-full"
                        isDisabled={serviceOptions.length === 0}
                        placeholder={t("home.dashboard.allServices")}
                        selectionMode="multiple"
                        value={selectedServiceIds}
                        onChange={(value) => setSelectedServiceIds(toSelectionValues(value))}
                      >
                        <Label>{t("home.dashboard.serviceFilterLabel")}</Label>
                        <Select.Trigger>
                          <Select.Value />
                          <Select.Indicator />
                        </Select.Trigger>
                        <Select.Popover>
                          <ListBox>
                            {serviceOptions.map((option) => (
                              <ListBox.Item key={option.value} id={option.value} textValue={option.label}>
                                {option.label}
                                <ListBox.ItemIndicator />
                              </ListBox.Item>
                            ))}
                          </ListBox>
                        </Select.Popover>
                      </Select>

                    </div>
                  </Popover.Dialog>
                </Popover.Content>
              </Popover>
            </div>
          </div>

          <div className="grid items-stretch gap-3 lg:grid-cols-[minmax(0,1fr)_auto]">
            <TextField aria-label={t("home.dashboard.searchLabel")} className={`${styles.searchField} min-w-0`}>
              <InputGroup className={`${styles.searchGroup} h-10`}>
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
            <div className={`${styles.dateControls} items-stretch gap-1`}>
              <Tooltip>
                <Button
                  aria-label={t("common.actions.previous")}
                  className={`${styles.dateNavButton} h-10 px-3 text-sm`}
                  variant="ghost"
                  onPress={() => moveDay(-1)}
                >
                  {t("common.actions.previous")}
                </Button>
                <Tooltip.Content>{t("common.actions.previous")}</Tooltip.Content>
              </Tooltip>
              <DatePicker
                aria-label={t("home.dashboard.datePickerLabel")}
                className={`${styles.datePicker} h-10`}
                granularity="day"
                value={focusedDate}
                onChange={(value) => {
                  if (value) setFocusedDate(value as CalendarDate);
                }}
              >
                <Label className="sr-only">{t("home.dashboard.datePickerLabel")}</Label>
                <DateField.Group className="h-full" fullWidth>
                  <DateField.Input>
                    {(segment) => <DateField.Segment segment={segment} />}
                  </DateField.Input>
                  <DateField.Suffix>
                    <DatePicker.Trigger aria-label={t("home.dashboard.openDatePicker")}>
                      <DatePicker.TriggerIndicator>
                        <i aria-hidden="true" className="fa-light fa-calendar-days" />
                      </DatePicker.TriggerIndicator>
                    </DatePicker.Trigger>
                  </DateField.Suffix>
                </DateField.Group>
                <DatePicker.Popover className="w-[24rem] max-w-[calc(100vw-2rem)]">
                  <Calendar
                    aria-label={t("home.dashboard.datePickerLabel")}
                    className="mx-auto w-full max-w-none"
                    onFocusChange={setFocusedDate}
                    pageBehavior="single"
                  >
                    {() => (
                      <>
                        <Calendar.Header>
                          <Calendar.NavButton
                            slot="previous"
                            aria-label={t("home.dashboard.previousMonth")}
                          >
                            <i
                              aria-hidden="true"
                              className={`fa-light ${direction === "rtl" ? "fa-chevron-right" : "fa-chevron-left"}`}
                            />
                          </Calendar.NavButton>
                          <Calendar.YearPickerTrigger
                            aria-label={t("home.dashboard.selectYear")}
                          >
                            <Calendar.YearPickerTriggerHeading />
                            <Calendar.YearPickerTriggerIndicator />
                          </Calendar.YearPickerTrigger>
                          <Calendar.NavButton
                            slot="next"
                            aria-label={t("home.dashboard.nextMonth")}
                          >
                            <i
                              aria-hidden="true"
                              className={`fa-light ${direction === "rtl" ? "fa-chevron-left" : "fa-chevron-right"}`}
                            />
                          </Calendar.NavButton>
                        </Calendar.Header>
                        <Calendar.YearPickerGrid
                          aria-label={t("home.dashboard.selectYear")}
                        >
                          <Calendar.YearPickerGridBody>
                            {({ year, formattedYear }) => (
                              <Calendar.YearPickerCell year={year}>
                                {formattedYear}
                              </Calendar.YearPickerCell>
                            )}
                          </Calendar.YearPickerGridBody>
                        </Calendar.YearPickerGrid>
                        <Calendar.Grid weekdayStyle="short">
                          <Calendar.GridHeader>
                            {(day) => <Calendar.HeaderCell>{day}</Calendar.HeaderCell>}
                          </Calendar.GridHeader>
                          <Calendar.GridBody>
                            {(date) => <Calendar.Cell date={date} />}
                          </Calendar.GridBody>
                        </Calendar.Grid>
                      </>
                    )}
                  </Calendar>
                </DatePicker.Popover>
              </DatePicker>
              <Tooltip>
                <Button
                  aria-label={t("common.actions.next")}
                  className={`${styles.dateNavButton} h-10 px-3 text-sm`}
                  variant="ghost"
                  onPress={() => moveDay(1)}
                >
                  {t("common.actions.next")}
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
                {activeFilterCount > 0
                  ? t("home.dashboard.noFilterResults")
                  : t("home.dashboard.noAppointments")}
              </p>
            </div>
          ) : (
            <div className={`${styles.table} overflow-hidden`}>
              <div>
                <div className={`${styles.tableHeader} ${styles.gridColumns} grid gap-2 border-b border-separator px-4 py-3 text-xs font-semibold sm:px-6 lg:px-8`}>
                  <span>{t("home.dashboard.patientColumn")}</span>
                  <span>{t("home.dashboard.serviceColumn")}</span>
                  <span>{t("home.dashboard.statusColumn")}</span>
                  <span>{t("home.dashboard.actionsColumn")}</span>
                </div>
                <div className="divide-y divide-separator/70">
                  {visibleRows.map((row) => {
                    const patientLabel = getPatientLabel(row.patientDisplayName, t);
                    const status = getStatusMeta(row.status, t);

                    return (
                      <div
                        className={`${styles.row} ${styles.gridColumns} grid items-center gap-2 px-4 py-3 transition-colors sm:px-6 lg:px-8`}
                        key={row.reservationId}
                      >
                        <div className="flex min-w-0 items-center gap-3">
                          <span className={`${identityColorClassName(identitySlotForId(row.patientId ?? row.reservationId))} ${styles.avatar} grid size-10 shrink-0 place-items-center rounded-full text-sm font-semibold`}>
                            {getInitials(patientLabel)}
                          </span>
                          <div className="min-w-0">
                            <p className="truncate text-sm font-semibold text-foreground">{patientLabel}</p>
                          </div>
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
                            <Button className={`${actionRoleClassName("open")} ${styles.actionButton}`} size="sm" variant="primary" onPress={() => openPatient(row.patientId)}>
                              {t("home.dashboard.viewProfile")}
                            </Button>
                            <Tooltip.Content>{t("home.dashboard.viewProfile")}</Tooltip.Content>
                          </Tooltip>
                          <Tooltip>
                            <Button className={`${actionRoleClassName("edit")} ${styles.actionButton}`} size="sm" variant="outline" onPress={() => openPatient(row.patientId)}>
                              {t("home.dashboard.newPrescription")}
                            </Button>
                            <Tooltip.Content>{t("home.dashboard.newPrescription")}</Tooltip.Content>
                          </Tooltip>
                          <Tooltip>
                            <Button className={`${actionRoleClassName("create")} ${styles.actionButton}`} size="sm" variant="ghost" onPress={() => openPatient(row.patientId)}>
                              {t("home.dashboard.newNote")}
                            </Button>
                            <Tooltip.Content>{t("home.dashboard.newNote")}</Tooltip.Content>
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

function toSelectionValues(value: unknown): string[] {
  if (value === null || value === undefined) return [];
  return Array.isArray(value) ? value.map(String) : [String(value)];
}

function getServiceFilterKey(
  serviceId: string | null,
  serviceDisplayName: string | null,
  locale: string,
) {
  return getFilterKey(serviceId, serviceDisplayName, locale, "missing-service");
}

function getFilterKey(
  id: string | null,
  displayName: string | null,
  locale: string,
  missingKey: string,
) {
  if (id?.trim()) return `id:${id.trim()}`;
  if (displayName?.trim()) return `name:${displayName.trim().toLocaleLowerCase(locale)}`;
  return missingKey;
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
      return { color: "default", label: t("clinic.reservationStatus.waitlisted") };
    case 6:
      return { color: "success", label: t("clinic.reservationStatus.present") };
    case 7:
      return { color: "success", label: t("clinic.reservationStatus.completed") };
    default:
      return { color: "default", label: t("home.dashboard.unknownStatus") };
  }
}
