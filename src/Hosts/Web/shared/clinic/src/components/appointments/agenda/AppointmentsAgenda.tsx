"use client";

import { useCallback, useMemo, useState } from "react";
import type { CSSProperties } from "react";
import { Agenda, Segment, useAgenda, Widget } from "@heroui-pro/react";
import { Button } from "@heroui/react";
import { contextColorClassName } from "@vitarish/ui";
import {
  createCalendar,
  getDayOfWeek,
  getLocalTimeZone,
  today,
  toCalendar,
} from "@internationalized/date";
import type { CalendarDate } from "@internationalized/date";
import type { NavigationIntent } from "../../../navigation";
import type {
  ShiftReadDto,
  ReservationReadDto,
} from "../../../api/appointments/appointmentsApi";
import { useAgendaEvents } from "../../../hooks/appointments/agenda/useAgendaEvents";
import { useBookableShiftOccurrences } from "../../../hooks/appointments/agenda/useBookableShiftOccurrences";
import { useReservationPartyNames } from "../../../hooks/appointments/agenda/useReservationPartyNames";
import { useServices } from "../../../hooks/services/useServices";
import { useServiceProviders } from "../../../hooks/service-providers/useServiceProviders";
import {
  formatDate,
  formatNumber,
  getDateCalendar,
  useLocalizedText,
} from "../../../localization";
import { AddReservationDialog } from "./AddReservationDialog";
import { ReservationDetailDialog } from "./ReservationDetailDialog";

interface AppointmentsAgendaProps {
  shifts: ShiftReadDto[];
  reservations: ReservationReadDto[];
  onNavigate?: (intent: NavigationIntent) => void;
}

function getVisibleDateStrings(
  date: CalendarDate,
  view: "day" | "week" | "month",
): string[] {
  const dates: string[] = [];
  if (view === "day") {
    dates.push(date.toString());
  } else if (view === "week") {
    const start = date.add({ days: -getDayOfWeek(date, "en-US") });
    for (let i = 0; i < 7; i++) {
      dates.push(start.add({ days: i }).toString());
    }
  } else {
    const start = date.add({ days: -date.day + 1 });
    const daysInMonth = date.calendar.getDaysInMonth(date);
    for (let i = 0; i < daysInMonth; i++) {
      dates.push(start.add({ days: i }).toString());
    }
  }
  return dates;
}

export function AppointmentsAgenda({
  shifts,
  reservations,
  onNavigate,
}: AppointmentsAgendaProps) {
  const { t, locale } = useLocalizedText();
  // Agenda uses the calendar carried by its CalendarDate for cell labels. Use
  // the calendar configured for the active locale so the date system remains
  // independent of any specific language.
  const [date, setDate] = useState(() =>
    toCalendar(
      today(getLocalTimeZone()),
      createCalendar(getDateCalendar(locale)),
    ),
  );
  const [view, setView] = useState<"day" | "week" | "month">("week");
  const [dialogOpen, setDialogOpen] = useState(false);
  const [selectedShift, setSelectedShift] = useState<ShiftReadDto | null>(null);
  const [selectedDate, setSelectedDate] = useState("");
  const [selectedAvailableSeats, setSelectedAvailableSeats] = useState(0);
  const [detailReservationId, setDetailReservationId] = useState<string | null>(
    null,
  );

  const visibleDates = useMemo(
    () => getVisibleDateStrings(date, view),
    [date, view],
  );

  const { capacityMap, occurrenceKeys } = useBookableShiftOccurrences(
    shifts,
    visibleDates,
  );
  const reservingPartyNames = useReservationPartyNames(reservations);
  const { data: services } = useServices();
  const { data: serviceProviders } = useServiceProviders();
  const formatShiftTime = useCallback(
    (time: string) =>
      formatDate(
        `${date.toString()}T${time.length === 5 ? `${time}:00` : time}`,
        locale,
        {
          hour: "numeric",
          minute: "2-digit",
        },
      ),
    [date, locale],
  );
  const events = useAgendaEvents(
    date,
    view,
    shifts,
    reservations,
    locale,
    capacityMap,
    occurrenceKeys,
    services,
    serviceProviders,
    t("clinic.fields.shift"),
    formatShiftTime,
    (available: number, total: number) =>
      `${formatNumber(available, locale)}/${formatNumber(total, locale)}`,
    reservingPartyNames,
  );

  const shiftMap = useMemo(
    () => new Map(shifts.map((s) => [s.id, s])),
    [shifts],
  );

  const openAddDialog = useCallback(
    (shiftId: string, dateStr: string) => {
      const shift = shiftMap.get(shiftId);
      if (!shift) return;
      const cap = capacityMap.get(`${shiftId}|${dateStr}`);
      setSelectedShift(shift);
      setSelectedDate(dateStr);
      setSelectedAvailableSeats(cap?.availableSeats ?? shift.totalSeats);
      setDialogOpen(true);
    },
    [shiftMap, capacityMap],
  );

  const handleAgendaEventSelect = useCallback(
    (id: string | null) => {
      if (!id) return;
      const [kind, shiftId, dateStr] = id.split("|");
      if (kind === "shift" && shiftId && dateStr) {
        openAddDialog(shiftId, dateStr);
        return;
      }
      setDetailReservationId(id);
    },
    [openAddDialog],
  );

  const agenda = useAgenda({
    events,
    view,
    date,
    onViewChange: setView,
    onDateChange: setDate,
    startHour: 7,
    endHour: 20,
    onEventSelect: handleAgendaEventSelect,
  });

  return (
    <Widget className={contextColorClassName("scheduling")}>
      <Widget.Content className="p-0">
        <Agenda {...agenda}>
          <Agenda.Header>
            <Agenda.Heading />
            <Agenda.ViewSelector>
              <Segment.Item id="day">
                {t("clinic.appointments.agenda.day")}
              </Segment.Item>
              <Segment.Item id="week">
                {t("clinic.appointments.agenda.week")}
              </Segment.Item>
              <Segment.Item id="month">
                {t("clinic.appointments.agenda.month")}
              </Segment.Item>
            </Agenda.ViewSelector>
            <Agenda.Navigation>
              <Button
                isIconOnly
                aria-label={t("clinic.appointments.agenda.previous")}
                className="agenda__nav-button"
                size="sm"
                variant="ghost"
                onPress={agenda.goToPrevious}
              >
                <span aria-hidden="true">‹</span>
              </Button>
              <Agenda.TodayButton>
                {t("clinic.appointments.agenda.today")}
              </Agenda.TodayButton>
              <Button
                isIconOnly
                aria-label={t("clinic.appointments.agenda.next")}
                className="agenda__nav-button"
                size="sm"
                variant="ghost"
                onPress={agenda.goToNext}
              >
                <span aria-hidden="true">›</span>
              </Button>
            </Agenda.Navigation>
          </Agenda.Header>
          <Agenda.Body>
            {view === "month" ? (
              <Agenda.MonthGrid>
                {agenda.visibleWeeks.map((week) => {
                  const rowLayout = agenda.getMonthRowLayout(week);

                  return (
                    <Agenda.MonthRow
                      key={week[0]?.toString()}
                      spanningRowCount={rowLayout.rowCount}
                    >
                      {rowLayout.items.map((item) => (
                        <Agenda.MonthSpanningEvent
                          key={item.event.id}
                          event={item.event}
                          colStart={item.colStart}
                          colSpan={item.colSpan}
                          row={item.row}
                        />
                      ))}
                      {week.map((day, columnIndex) => (
                        <Agenda.MonthCell
                          key={day.toString()}
                          date={day}
                          moreLabel={(count) =>
                            t("clinic.appointments.agenda.more", {
                              count: formatNumber(count, locale),
                            })
                          }
                          spanningRowCount={
                            rowLayout.rowCountPerCol[columnIndex] ?? 0
                          }
                        >
                          {agenda.getPerCellEvents(day, week).map((event) => (
                            <Agenda.MonthEvent key={event.id} event={event} />
                          ))}
                        </Agenda.MonthCell>
                      ))}
                    </Agenda.MonthRow>
                  );
                })}
              </Agenda.MonthGrid>
            ) : (
              <>
                <Agenda.WeekHeader>
                  {agenda.visibleDays.map((day) => (
                    <div key={day.toString()} className="agenda__day-header">
                      <span className="agenda__day-header-name">
                        {formatDate(day.toDate(getLocalTimeZone()), locale, {
                          weekday: "short",
                        })}
                      </span>
                      <span className="agenda__day-header-date">
                        {formatNumber(day.day, locale)}
                      </span>
                    </div>
                  ))}
                </Agenda.WeekHeader>
                <Agenda.AllDaySection>
                  <Agenda.AllDayLabel>
                    {t("clinic.appointments.agenda.allDay")}
                  </Agenda.AllDayLabel>
                  {agenda.allDayLayout.map((item) => (
                    <Button
                      key={item.event.id}
                      type="button"
                      className="agenda__all-day-event h-auto min-h-8 min-w-0 justify-start overflow-hidden px-2 py-1 text-start"
                      style={
                        {
                          gridColumn: `${item.colStart + 1} / span ${item.colSpan}`,
                          gridRow: item.row + 1,
                          ...(item.event.color
                            ? { "--agenda-event-accent": item.event.color }
                            : {}),
                        } as CSSProperties
                      }
                      variant="ghost"
                      aria-label={`${t("clinic.appointments.agenda.bookShift")}: ${item.event.title}`}
                      onPress={() => handleAgendaEventSelect(item.event.id)}
                    >
                      <span className="truncate">{item.event.title}</span>
                    </Button>
                  ))}
                </Agenda.AllDaySection>
                <Agenda.TimeGrid>
                  <Agenda.CurrentTimeIndicator />
                  {agenda.visibleDays.map((day) => (
                    <Agenda.DayColumn key={day.toString()} date={day}>
                      {agenda.getEventsForDay(day).map((event) => (
                        <Agenda.Event key={event.id} event={event} />
                      ))}
                    </Agenda.DayColumn>
                  ))}
                </Agenda.TimeGrid>
              </>
            )}
          </Agenda.Body>
        </Agenda>
      </Widget.Content>

      {selectedShift && (
        <AddReservationDialog
          isOpen={dialogOpen}
          onClose={() => setDialogOpen(false)}
          shift={selectedShift}
          date={selectedDate}
          availableSeats={selectedAvailableSeats}
        />
      )}

      {detailReservationId && (
        <ReservationDetailDialog
          isOpen={!!detailReservationId}
          onClose={() => setDetailReservationId(null)}
          reservationId={detailReservationId}
          onViewPatient={(patientId) =>
            onNavigate?.({ kind: "patientDetail", patientId })
          }
        />
      )}
    </Widget>
  );
}
