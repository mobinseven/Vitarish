"use client";

import { useCallback, useMemo, useState } from "react";
import { Agenda, Segment, useAgenda } from "@heroui-pro/react";
import { Button } from "@heroui/react";
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
import { useShiftCapacity } from "../../../hooks/appointments/useShiftCapacity";
import { useServices } from "../../../hooks/services/useServices";
import { useServiceProviders } from "../../../hooks/service-providers/useServiceProviders";
import {
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

function getVisibleDateStrings(date: CalendarDate, view: "day" | "week" | "month"): string[] {
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
  const [detailReservationId, setDetailReservationId] = useState<string | null>(null);

  const visibleDates = useMemo(
    () => getVisibleDateStrings(date, view),
    [date, view],
  );

  const capacityMap = useShiftCapacity(shifts, reservations, visibleDates);
  const { data: services } = useServices();
  const { data: serviceProviders } = useServiceProviders();
  const events = useAgendaEvents(date, view, shifts, reservations, locale, capacityMap, services, serviceProviders);

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

  const agenda = useAgenda({
    events,
    view,
    date,
    onViewChange: setView,
    onDateChange: setDate,
    startHour: 7,
    endHour: 20,
    onEventSelect: (id) => {
      if (!id) return;
      if (id.startsWith("shift|")) {
        // Format: "shift|{shiftId}|{date}"
        const parts = id.split("|");
        const shiftId = parts[1];
        const dateStr = parts[2];
        openAddDialog(shiftId, dateStr);
      } else {
        setDetailReservationId(id);
      }
    },
  });

  return (
    <div className="flex flex-col gap-4">
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
              <Agenda.WeekHeader />
              <Agenda.AllDaySection>
                <Agenda.AllDayLabel>
                  {t("clinic.appointments.agenda.allDay")}
                </Agenda.AllDayLabel>
                {agenda.allDayLayout.map((item) => (
                  <Agenda.AllDayEvent
                    key={item.event.id}
                    event={item.event}
                    colStart={item.colStart}
                    colSpan={item.colSpan}
                    row={item.row}
                  />
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
    </div>
  );
}
