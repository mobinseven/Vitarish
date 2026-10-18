"use client";

import { useCallback, useMemo, useState } from "react";
import { createCalendar, getLocalTimeZone, today, toCalendar } from "@internationalized/date";
import type { CalendarDate } from "@internationalized/date";
import { Button, Chip, Input, Label, ListBox, Select, Surface, Tag, TagGroup, TextField, Tooltip } from "@heroui/react";
import { Segment } from "@heroui-pro/react";
import {
  AnchorPageHeader,
  EmptyStateSurface as ClinicEmptyState,
  ErrorStateSurface as ClinicErrorState,
  FaIcon,
  HeaderIcon,
  LoadingStateSurface as ClinicLoadingState,
} from "@vitarish/ui";
import type { NavigationIntent } from "../../navigation";
import { dateValueToIsoDate, formatDate, formatNumber, getDateCalendar, useLocalizedText } from "../../localization";
import { useCanPlanSurgeries } from "../../hooks/surgeries/useCanPlanSurgeries";
import { useClinicStaff } from "../../hooks/surgeries/useClinicStaff";
import { useSurgeryCalendar } from "../../hooks/surgeries/useSurgeryCalendar";
import { surgeryCalendarRange, surgeryDayKey } from "../../models/surgeries/surgeryCalendarRange";
import type { SurgeryCalendarView } from "../../models/surgeries/surgeryCalendarRange";
import { surgeryStatuses } from "../../models/surgeries/surgeryModels";
import type { Surgery, SurgeryStatus } from "../../models/surgeries/surgeryModels";
import { SurgeryCalendarGrid } from "./SurgeryCalendarGrid";
import { SurgeryList } from "./SurgeryList";
import { SurgeryStatusChip, surgeryStatusLabelKeys } from "./SurgeryStatusChip";

export interface SurgeryCalendarPageProps {
  onNavigate: (intent: NavigationIntent) => void;
}

type StatusFilter = "all" | SurgeryStatus;

const filterableStatuses: SurgeryStatus[] = [
  surgeryStatuses.planned,
  surgeryStatuses.completed,
  surgeryStatuses.cancelled,
];

// The status chip itself is the filter option; the selected one is ringed.
const statusFilterTagClassName =
  "rounded-full bg-transparent p-0 ring-offset-2 ring-offset-surface data-[selected=true]:ring-2 data-[selected=true]:ring-foreground/40";

export function SurgeryCalendarPage({ onNavigate }: SurgeryCalendarPageProps) {
  const { t, locale, direction } = useLocalizedText();
  const canPlan = useCanPlanSurgeries();
  const timeZone = getLocalTimeZone();
  const [view, setView] = useState<SurgeryCalendarView>("month");
  const [focusedDate, setFocusedDate] = useState<CalendarDate>(() =>
    toCalendar(today(timeZone), createCalendar(getDateCalendar(locale))),
  );
  const [selectedDay, setSelectedDay] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("all");
  const [teamFilter, setTeamFilter] = useState("");
  const [locationFilter, setLocationFilter] = useState("");

  const range = useMemo(
    () => surgeryCalendarRange(view, focusedDate, timeZone, locale),
    [view, focusedDate, timeZone, locale],
  );
  const { data, isLoading, error, refetch } = useSurgeryCalendar({ from: range.from, to: range.to });
  const { data: staff } = useClinicStaff();

  const normalizedLocation = locationFilter.trim().toLowerCase();
  const filtered = useMemo(() => {
    const source = data ?? [];
    return source.filter((surgery) => {
      if (statusFilter !== "all" && surgery.status !== statusFilter) return false;
      if (teamFilter && !surgery.team.some((member) => member.userId === teamFilter)) return false;
      if (normalizedLocation && !(surgery.location ?? "").toLowerCase().includes(normalizedLocation)) return false;
      return true;
    });
  }, [data, statusFilter, teamFilter, normalizedLocation]);

  const surgeriesByDay = useMemo(() => {
    const map = new Map<string, Surgery[]>();
    for (const surgery of filtered) {
      const key = surgeryDayKey(surgery.scheduledAt, timeZone);
      const bucket = map.get(key);
      if (bucket) bucket.push(surgery);
      else map.set(key, [surgery]);
    }
    for (const bucket of map.values()) {
      bucket.sort((a, b) => a.scheduledAt.localeCompare(b.scheduledAt));
    }
    return map;
  }, [filtered, timeZone]);

  const orderedDayKeys = useMemo(() => {
    if (selectedDay) return [selectedDay];
    return range.days.map((day) => dateValueToIsoDate(day)).filter((key) => surgeriesByDay.has(key));
  }, [range.days, selectedDay, surgeriesByDay]);

  const selectedDayDate = useMemo(() => {
    if (!selectedDay) return null;
    return range.days.find((day) => dateValueToIsoDate(day) === selectedDay) ?? null;
  }, [range.days, selectedDay]);

  const todayKey = useMemo(() => dateValueToIsoDate(today(timeZone)), [timeZone]);
  const hasActiveFilter = statusFilter !== "all" || teamFilter !== "" || normalizedLocation !== "";

  const activeFilters = useMemo(() => {
    const chips: { key: string; label: string; onRemove: () => void }[] = [];
    if (statusFilter !== "all") {
      chips.push({
        key: "status",
        label: t("clinic.surgeries.calendar.filters.statusChip", { value: t(surgeryStatusLabelKeys[statusFilter]) }),
        onRemove: () => setStatusFilter("all"),
      });
    }
    if (teamFilter) {
      const member = (staff ?? []).find((candidate) => candidate.userId === teamFilter);
      chips.push({
        key: "team",
        label: t("clinic.surgeries.calendar.filters.teamChip", { value: member?.displayName ?? teamFilter }),
        onRemove: () => setTeamFilter(""),
      });
    }
    if (normalizedLocation) {
      chips.push({
        key: "location",
        label: t("clinic.surgeries.calendar.filters.locationChip", { value: locationFilter.trim() }),
        onRemove: () => setLocationFilter(""),
      });
    }
    return chips;
  }, [statusFilter, teamFilter, locationFilter, normalizedLocation, staff, t]);

  const clearFilters = useCallback(() => {
    setStatusFilter("all");
    setTeamFilter("");
    setLocationFilter("");
  }, []);

  const changeView = useCallback((nextView: SurgeryCalendarView) => {
    setSelectedDay(null);
    setView(nextView);
  }, []);

  const goToPrevious = useCallback(() => {
    setSelectedDay(null);
    setFocusedDate((current) => current.add(view === "month" ? { months: -1 } : { days: -7 }));
  }, [view]);

  const goToNext = useCallback(() => {
    setSelectedDay(null);
    setFocusedDate((current) => current.add(view === "month" ? { months: 1 } : { days: 7 }));
  }, [view]);

  const goToToday = useCallback(() => {
    setSelectedDay(null);
    setFocusedDate(toCalendar(today(timeZone), createCalendar(getDateCalendar(locale))));
  }, [locale, timeZone]);

  const toggleDay = useCallback((dayKey: string) => {
    setSelectedDay((current) => (current === dayKey ? null : dayKey));
  }, []);

  const periodLabel = formatDate(focusedDate.toDate(timeZone), locale, { month: "long", year: "numeric" });
  const periodCountLabel = t(
    filtered.length === 1
      ? "clinic.surgeries.calendar.periodCount.one"
      : "clinic.surgeries.calendar.periodCount.other",
    { count: formatNumber(filtered.length, locale) },
  );

  return (
    <div className="mx-auto flex w-full max-w-7xl flex-col gap-6">
      <AnchorPageHeader
        icon="scissors"
        title={t("clinic.surgeries.title")}
        details={t("clinic.surgeries.calendar.pageDescription")}
        actions={canPlan ? (
          <Tooltip>
            <Button
              aria-label={t("clinic.surgeries.actions.plan")}
              variant="primary"
              onPress={() => onNavigate({ kind: "surgeryNew" })}
            >
              <FaIcon icon="plus" aria-hidden="true" />
              <span className="hidden sm:inline">{t("clinic.surgeries.actions.plan")}</span>
            </Button>
            <Tooltip.Content>{t("clinic.surgeries.actions.plan")}</Tooltip.Content>
          </Tooltip>
        ) : null}
      />

      {isLoading ? <ClinicLoadingState label={t("clinic.surgeries.calendar.loading")} /> : null}

      {error ? (
        <ClinicErrorState title={t("clinic.surgeries.errors.loadFailed")}>
          <Tooltip>
            <Button variant="outline" onPress={() => void refetch()}>
              <FaIcon icon="rotate-right" aria-hidden="true" />
              {t("clinic.surgeries.actions.retry")}
            </Button>
            <Tooltip.Content>{t("clinic.surgeries.actions.retry")}</Tooltip.Content>
          </Tooltip>
        </ClinicErrorState>
      ) : null}

      {!isLoading && !error ? (
        <>
          <Surface className="flex flex-col gap-6 rounded-3xl p-6">
            <div className="flex flex-col items-start gap-5 sm:flex-row sm:items-center sm:justify-between">
              <div className="flex items-center gap-3">
                <HeaderIcon icon="calendar" />
                <div className="min-w-0">
                  <h2 className="text-lg font-semibold text-foreground">
                    {t("clinic.surgeries.calendar.calendarTitle")}
                  </h2>
                  <p className="text-sm text-muted">{periodCountLabel}</p>
                </div>
              </div>
              <div className="flex flex-wrap items-center gap-2">
                <span className="whitespace-nowrap text-sm font-medium text-foreground">{periodLabel}</span>
                <Segment
                  aria-label={t("clinic.surgeries.calendar.view.label")}
                  size="sm"
                  selectedKey={view}
                  onSelectionChange={(key) => changeView(key as SurgeryCalendarView)}
                >
                  <Segment.Item id="month">{t("clinic.surgeries.calendar.view.month")}</Segment.Item>
                  <Segment.Item id="week">{t("clinic.surgeries.calendar.view.week")}</Segment.Item>
                </Segment>
                <div className="flex items-center gap-1">
                  <Tooltip>
                    <Button
                      isIconOnly
                      aria-label={t("clinic.surgeries.calendar.navigation.previous")}
                      variant="outline"
                      onPress={goToPrevious}
                    >
                      <FaIcon icon={direction === "rtl" ? "chevron-right" : "chevron-left"} aria-hidden="true" />
                    </Button>
                    <Tooltip.Content>{t("clinic.surgeries.calendar.navigation.previous")}</Tooltip.Content>
                  </Tooltip>
                  <Tooltip>
                    <Button variant="outline" onPress={goToToday}>
                      <FaIcon icon="calendar-day" aria-hidden="true" />
                      <span className="hidden sm:inline">{t("clinic.surgeries.calendar.navigation.today")}</span>
                    </Button>
                    <Tooltip.Content>{t("clinic.surgeries.calendar.navigation.today")}</Tooltip.Content>
                  </Tooltip>
                  <Tooltip>
                    <Button
                      isIconOnly
                      aria-label={t("clinic.surgeries.calendar.navigation.next")}
                      variant="outline"
                      onPress={goToNext}
                    >
                      <FaIcon icon={direction === "rtl" ? "chevron-left" : "chevron-right"} aria-hidden="true" />
                    </Button>
                    <Tooltip.Content>{t("clinic.surgeries.calendar.navigation.next")}</Tooltip.Content>
                  </Tooltip>
                </div>
              </div>
            </div>

            <div className="flex flex-col gap-4">
              <div
                className="flex flex-wrap items-end gap-3"
                role="group"
                aria-label={t("clinic.surgeries.calendar.filters.title")}
              >
                <div className="min-w-0">
                  <p className={`mb-1.5 text-sm ${statusFilter !== "all" ? "text-accent" : "text-muted"}`}>
                    {t("clinic.surgeries.calendar.filters.status")}
                  </p>
                  <TagGroup
                    aria-label={t("clinic.surgeries.calendar.filters.status")}
                    selectionMode="single"
                    selectedKeys={new Set([statusFilter])}
                    onSelectionChange={(keys) => {
                      // Pressing the selected chip again keeps it; another chip replaces it.
                      const [key] = Array.from(keys);
                      if (key !== undefined) setStatusFilter(key as StatusFilter);
                    }}
                  >
                    <TagGroup.List className="gap-2">
                      <Tag
                        id="all"
                        className={statusFilterTagClassName}
                        textValue={t("clinic.surgeries.calendar.filters.statusAll")}
                      >
                        <Chip size="sm">
                          <FaIcon aria-hidden="true" icon="layer-group" />
                          <Chip.Label>{t("clinic.surgeries.calendar.filters.statusAll")}</Chip.Label>
                        </Chip>
                      </Tag>
                      {filterableStatuses.map((status) => (
                        <Tag
                          key={status}
                          id={status}
                          className={statusFilterTagClassName}
                          textValue={t(surgeryStatusLabelKeys[status])}
                        >
                          <SurgeryStatusChip status={status} />
                        </Tag>
                      ))}
                    </TagGroup.List>
                  </TagGroup>
                </div>
                <div className="flex min-w-44 flex-col gap-1.5">
                  <Label className={teamFilter ? "text-accent" : undefined}>
                    {t("clinic.surgeries.calendar.filters.teamMember")}
                  </Label>
                  <Select
                    aria-label={t("clinic.surgeries.calendar.filters.teamMember")}
                    className="min-w-0"
                    placeholder={t("clinic.surgeries.calendar.filters.teamMemberAll")}
                    value={teamFilter}
                    variant="secondary"
                    onChange={(nextValue) => setTeamFilter(nextValue === null ? "" : String(nextValue))}
                  >
                    <Select.Trigger>
                      <Select.Value className="min-w-0 overflow-hidden text-ellipsis whitespace-nowrap" />
                      <Select.Indicator />
                    </Select.Trigger>
                    <Select.Popover>
                      <ListBox>
                        <ListBox.Item id="" textValue={t("clinic.surgeries.calendar.filters.teamMemberAll")}>
                          {t("clinic.surgeries.calendar.filters.teamMemberAll")}
                          <ListBox.ItemIndicator />
                        </ListBox.Item>
                        {(staff ?? []).map((member) => (
                          <ListBox.Item key={member.userId} id={member.userId} textValue={member.displayName}>
                            {member.displayName}
                            <ListBox.ItemIndicator />
                          </ListBox.Item>
                        ))}
                      </ListBox>
                    </Select.Popover>
                  </Select>
                </div>
                <TextField className="min-w-0" value={locationFilter} onChange={setLocationFilter}>
                  <Label className={locationFilter.trim() ? "text-accent" : undefined}>
                    {t("clinic.surgeries.calendar.filters.location")}
                  </Label>
                  <Input placeholder={t("clinic.surgeries.calendar.filters.locationPlaceholder")} />
                </TextField>
                {hasActiveFilter ? (
                  <Tooltip>
                    <Button variant="ghost" onPress={clearFilters}>
                      <FaIcon icon="filter-circle-xmark" aria-hidden="true" />
                      <span className="hidden sm:inline">{t("clinic.surgeries.calendar.filters.clear")}</span>
                    </Button>
                    <Tooltip.Content>{t("clinic.surgeries.calendar.filters.clear")}</Tooltip.Content>
                  </Tooltip>
                ) : null}
              </div>
              {activeFilters.length > 0 ? (
                <div className="flex flex-wrap items-center gap-2" aria-label={t("clinic.surgeries.calendar.filters.active")}>
                  <span className="text-sm text-muted">{t("clinic.surgeries.calendar.filters.active")}</span>
                  {activeFilters.map((filter) => (
                    <div key={filter.key} className="flex items-center gap-0.5">
                      <Chip color="accent" size="sm" variant="soft">
                        <Chip.Label>{filter.label}</Chip.Label>
                      </Chip>
                      <Tooltip>
                        <Button
                          isIconOnly
                          size="sm"
                          variant="ghost"
                          aria-label={t("clinic.surgeries.calendar.filters.remove", { filter: filter.label })}
                          onPress={filter.onRemove}
                        >
                          <FaIcon icon="xmark" aria-hidden="true" />
                        </Button>
                        <Tooltip.Content>
                          {t("clinic.surgeries.calendar.filters.remove", { filter: filter.label })}
                        </Tooltip.Content>
                      </Tooltip>
                    </div>
                  ))}
                </div>
              ) : null}
            </div>

            <SurgeryCalendarGrid
              view={view}
              days={range.days}
              surgeriesByDay={surgeriesByDay}
              selectedDay={selectedDay}
              focusedDate={focusedDate}
              todayKey={todayKey}
              timeZone={timeZone}
              onSelectDay={toggleDay}
            />
          </Surface>

          <Surface
            aria-label={t("clinic.surgeries.calendar.listLabel")}
            className="flex flex-col gap-6 rounded-3xl p-6"
            role="region"
          >
            <div className="flex items-center gap-3">
              <HeaderIcon icon="list" />
              <div className="min-w-0">
                <h2 className="text-lg font-semibold text-foreground">
                  {selectedDay && selectedDayDate
                    ? t("clinic.surgeries.calendar.selectedDayTitle", {
                        date: formatDate(selectedDayDate.toDate(timeZone), locale, { dateStyle: "full" }),
                      })
                    : t("clinic.surgeries.calendar.rangeTitle")}
                </h2>
              </div>
            </div>
            {orderedDayKeys.length > 0 ? (
              <div className="flex flex-col gap-6">
                {orderedDayKeys.map((dayKey) => {
                  const daySurgeries = surgeriesByDay.get(dayKey) ?? [];
                  const dayDate = range.days.find((day) => dateValueToIsoDate(day) === dayKey);
                  return (
                    <div key={dayKey} className="flex flex-col gap-2">
                      {!selectedDay ? (
                        <h3 className="text-sm font-medium text-muted">
                          {t("clinic.surgeries.calendar.dayHeading", {
                            date: dayDate
                              ? formatDate(dayDate.toDate(timeZone), locale, { dateStyle: "full" })
                              : dayKey,
                          })}
                        </h3>
                      ) : null}
                      <SurgeryList
                        surgeries={daySurgeries}
                        emptyMessage={
                          selectedDay
                            ? t("clinic.surgeries.calendar.emptyDay")
                            : t("clinic.surgeries.calendar.empty")
                        }
                        onOpen={(surgery) => onNavigate({ kind: "surgeryDetail", surgeryId: surgery.id })}
                      />
                    </div>
                  );
                })}
              </div>
            ) : (
              <ClinicEmptyState
                title={
                  selectedDay
                    ? t("clinic.surgeries.calendar.emptyDay")
                    : t("clinic.surgeries.calendar.empty")
                }
              />
            )}
          </Surface>
        </>
      ) : null}
    </div>
  );
}
