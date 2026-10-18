"use client";

import { useMemo, useState } from "react";
import { Button, Chip, Label, ListBox, Popover, Select, Surface, Tooltip } from "@heroui/react";
import { Segment } from "@heroui-pro/react";
import { createCalendar, getLocalTimeZone, today as todayIn, toCalendar, type CalendarDate } from "@internationalized/date";
import { FaIcon, HeaderIcon } from "@vitarish/ui";
import { useClinicListDashboardAppointments } from "../../../api/generated/clinic/clinic";
import type { NavigationIntent } from "../../../navigation";
import { formatNumber, getDateCalendar, useLocalizedText } from "../../../localization";
import { visitPeriodRange, visitPeriods, type VisitPeriod } from "../../../models/appointments/visitPeriod";
import {
  activeVisitFilterCount,
  emptyVisitFilters,
  selectVisits,
  visitServiceKey,
  visitServiceLabel,
  visitStatusLabel,
  type VisitFilters,
  type VisitSortMode,
} from "../../../models/appointments/visitRows";
import { narrowingControlClassName, VisitDateStepper } from "./VisitDateStepper";
import { VisitResults, VisitSearchField } from "./VisitResults";

export interface VisitBrowserProps {
  onNavigate: (intent: NavigationIntent) => void;
}

type FilterOption = { value: string; label: string };

const defaultSortMode: VisitSortMode = "time";

/** Every visit of a day, week, or month, with search, status and service filters, and sorting. */
export function VisitBrowser({ onNavigate }: VisitBrowserProps) {
  const { t, locale } = useLocalizedText();
  const today = useMemo(
    () => toCalendar(todayIn(getLocalTimeZone()), createCalendar(getDateCalendar(locale))),
    [locale],
  );
  const [period, setPeriod] = useState<VisitPeriod>("day");
  const [focusedDate, setFocusedDate] = useState<CalendarDate>(today);
  const [filters, setFilters] = useState<VisitFilters>(emptyVisitFilters);
  const [sortMode, setSortMode] = useState<VisitSortMode>(defaultSortMode);
  const range = useMemo(() => visitPeriodRange(period, focusedDate, locale), [period, focusedDate, locale]);
  const query = useClinicListDashboardAppointments(range);
  const visits = useMemo(() => query.data ?? [], [query.data]);
  const shown = useMemo(
    () => selectVisits(visits, filters, sortMode, locale, t),
    [visits, filters, sortMode, locale, t],
  );
  const statusOptions = useMemo<FilterOption[]>(
    () =>
      [...new Set(visits.map((visit) => visit.status))]
        .sort((left, right) => left - right)
        .map((status) => ({ value: String(status), label: visitStatusLabel(status, t) })),
    [visits, t],
  );
  const serviceOptions = useMemo<FilterOption[]>(() => {
    const options = new Map<string, string>();
    for (const visit of visits) options.set(visitServiceKey(visit, locale), visitServiceLabel(visit, locale, t));
    return [...options]
      .map(([value, label]) => ({ value, label }))
      .sort((left, right) => left.label.localeCompare(right.label, locale));
  }, [visits, locale, t]);
  const filterCount = activeVisitFilterCount({ ...filters, search: "" });
  const sortLabel = sortMode === "time"
    ? t("clinic.appointments.visits.sortByTime")
    : t("clinic.appointments.visits.sortByPatient");

  // Filter options come from the shown period, so a new period starts unfiltered.
  function changeFocusedDate(date: CalendarDate) {
    setFocusedDate(date);
    setFilters((current) => ({ ...current, statuses: [], serviceKeys: [] }));
  }

  function changePeriod(next: VisitPeriod) {
    setPeriod(next);
    setFilters((current) => ({ ...current, statuses: [], serviceKeys: [] }));
  }

  return (
    <Surface className="flex flex-col gap-6 rounded-3xl p-6">
      <div className="flex flex-col items-start gap-5 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-3">
          <HeaderIcon icon="calendar-check" />
          <div className="min-w-0">
            <h2 className="text-lg font-semibold text-foreground">{t("clinic.appointments.visits.title")}</h2>
            <p className="text-sm text-muted">
              {t("clinic.appointments.visits.count", { count: formatNumber(shown.length, locale) })}
            </p>
          </div>
        </div>
        <div aria-label={t("clinic.appointments.visits.controlsLabel")} className="flex flex-wrap items-center gap-2" role="group">
          <Segment
            aria-label={t("clinic.appointments.visits.periodLabel")}
            selectedKey={period}
            size="sm"
            variant="ghost"
            onSelectionChange={(key) => changePeriod(key as VisitPeriod)}
          >
            {visitPeriods.map((option) => (
              <Segment.Item key={option} id={option}>
                {t(`clinic.appointments.visits.${option}`)}
              </Segment.Item>
            ))}
          </Segment>
          <Tooltip>
            <Button
              className={sortMode === defaultSortMode ? undefined : narrowingControlClassName}
              size="sm"
              variant="tertiary"
              onPress={() => setSortMode((current) => (current === "time" ? "patient" : "time"))}
            >
              <FaIcon aria-hidden="true" icon="arrow-down-a-z" />
              <span className="hidden sm:inline">{sortLabel}</span>
            </Button>
            <Tooltip.Content>{sortLabel}</Tooltip.Content>
          </Tooltip>
          <Popover>
            <Tooltip>
              <Button
                aria-label={t("clinic.appointments.visits.filters")}
                className={filterCount > 0 ? narrowingControlClassName : undefined}
                size="sm"
                variant="tertiary"
              >
                <FaIcon aria-hidden="true" icon="filter" />
                <span className="hidden sm:inline">{t("clinic.appointments.visits.filters")}</span>
                {filterCount > 0 ? (
                  <Chip
                    aria-label={t("clinic.appointments.visits.activeFilterCount", { count: formatNumber(filterCount, locale) })}
                    color="accent"
                    size="sm"
                    variant="primary"
                  >
                    <Chip.Label>{formatNumber(filterCount, locale)}</Chip.Label>
                  </Chip>
                ) : null}
              </Button>
              <Tooltip.Content>{t("clinic.appointments.visits.filters")}</Tooltip.Content>
            </Tooltip>
            <Popover.Content className="w-[22rem] max-w-[calc(100vw-2rem)]">
              <Popover.Dialog className="flex flex-col gap-4 p-4">
                <div className="flex items-center justify-between gap-3">
                  <Popover.Heading className="font-semibold">
                    {t("clinic.appointments.visits.filterHeading")}
                  </Popover.Heading>
                  {filterCount > 0 ? (
                    <Tooltip>
                      <Button
                        size="sm"
                        variant="ghost"
                        onPress={() => setFilters((current) => ({ ...current, statuses: [], serviceKeys: [] }))}
                      >
                        <FaIcon aria-hidden="true" icon="rotate-left" />
                        {t("clinic.appointments.visits.clearFilters")}
                      </Button>
                      <Tooltip.Content>{t("clinic.appointments.visits.clearFilters")}</Tooltip.Content>
                    </Tooltip>
                  ) : null}
                </div>
                <FilterSelect
                  label={t("clinic.appointments.visits.statusFilterLabel")}
                  options={statusOptions}
                  placeholder={t("clinic.appointments.visits.allStatuses")}
                  value={filters.statuses}
                  onChange={(statuses) => setFilters((current) => ({ ...current, statuses }))}
                />
                <FilterSelect
                  label={t("clinic.appointments.visits.serviceFilterLabel")}
                  options={serviceOptions}
                  placeholder={t("clinic.appointments.visits.allServices")}
                  value={filters.serviceKeys}
                  onChange={(serviceKeys) => setFilters((current) => ({ ...current, serviceKeys }))}
                />
              </Popover.Dialog>
            </Popover.Content>
          </Popover>
        </div>
      </div>

      <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <VisitSearchField
          className="lg:max-w-md lg:flex-1"
          value={filters.search}
          onChange={(search) => setFilters((current) => ({ ...current, search }))}
        />
        <VisitDateStepper focusedDate={focusedDate} period={period} today={today} onFocusedDateChange={changeFocusedDate} />
      </div>

      <div className="-mx-6 -mb-6 overflow-hidden rounded-b-3xl border-t border-separator">
        <VisitResults
          emptyMessage={period === "day"
            ? t("clinic.appointments.visits.emptyDay")
            : t("clinic.appointments.visits.emptyPeriod")}
          isFiltered={visits.length > 0 && activeVisitFilterCount(filters) > 0}
          query={query}
          showDate={period !== "day"}
          visits={shown}
          onNavigate={onNavigate}
        />
      </div>
    </Surface>
  );
}

interface FilterSelectProps {
  label: string;
  placeholder: string;
  options: FilterOption[];
  value: string[];
  onChange: (value: string[]) => void;
}

function FilterSelect({ label, placeholder, options, value, onChange }: FilterSelectProps) {
  return (
    <Select
      aria-label={label}
      className="w-full"
      isDisabled={options.length === 0}
      placeholder={placeholder}
      selectionMode="multiple"
      value={value}
      onChange={(next) => onChange(next === null || next === undefined ? [] : (Array.isArray(next) ? next : [next]).map(String))}
    >
      <Label>{label}</Label>
      <Select.Trigger>
        <Select.Value />
        <Select.Indicator />
      </Select.Trigger>
      <Select.Popover>
        <ListBox>
          {options.map((option) => (
            <ListBox.Item key={option.value} id={option.value} textValue={option.label}>
              {option.label}
              <ListBox.ItemIndicator />
            </ListBox.Item>
          ))}
        </ListBox>
      </Select.Popover>
    </Select>
  );
}
