"use client";

import { useState } from "react";
import { Agenda, Segment, useAgenda } from "@heroui-pro/react";
import { Button } from "@heroui/react";
import { today, getLocalTimeZone } from "@internationalized/date";
import type { NavigationIntent } from "../../../navigation";
import type { ShiftReadDto, ReservationReadDto } from "../../../api/appointments/appointmentsApi";
import { useAgendaEvents } from "../../../hooks/appointments/agenda/useAgendaEvents";
import { formatNumber, useLocalizedText } from "../../../localization";

interface AppointmentsAgendaProps {
  shifts: ShiftReadDto[];
  reservations: ReservationReadDto[];
  onNavigate?: (intent: NavigationIntent) => void;
}

export function AppointmentsAgenda({ shifts, reservations, onNavigate }: AppointmentsAgendaProps) {
  const { t, locale } = useLocalizedText();
  const [date, setDate] = useState(today(getLocalTimeZone()));
  const [view, setView] = useState<"day" | "week" | "month">("week");

  const events = useAgendaEvents(date, view, shifts, reservations, locale);

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
      if (id.startsWith("shift-")) {
        const shiftId = id.split("-")[1];
        onNavigate?.({ kind: "ownerShiftDetail", shiftId });
      } else {
        onNavigate?.({ kind: "ownerReservationDetail", reservationId: id });
      }
    },
  });

  return (
    <Agenda {...agenda}>
      <Agenda.Header>
        <Agenda.Heading />
        <Agenda.ViewSelector>
          <Segment.Item id="day">{t("clinic.appointments.agenda.day")}</Segment.Item>
          <Segment.Item id="week">{t("clinic.appointments.agenda.week")}</Segment.Item>
          <Segment.Item id="month">{t("clinic.appointments.agenda.month")}</Segment.Item>
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
          <Agenda.TodayButton>{t("clinic.appointments.agenda.today")}</Agenda.TodayButton>
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
                <Agenda.MonthRow key={week[0]?.toString()} spanningRowCount={rowLayout.rowCount}>
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
                      moreLabel={(count) => t("clinic.appointments.agenda.more", {
                        count: formatNumber(count, locale),
                      })}
                      spanningRowCount={rowLayout.rowCountPerCol[columnIndex] ?? 0}
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
              <Agenda.AllDayLabel>{t("clinic.appointments.agenda.allDay")}</Agenda.AllDayLabel>
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
  );
}
