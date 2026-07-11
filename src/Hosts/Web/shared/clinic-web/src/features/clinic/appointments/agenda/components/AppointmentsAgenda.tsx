"use client";

import { useState } from "react";
import { Agenda, useAgenda } from "@heroui-pro/react";
import { today, getLocalTimeZone } from "@internationalized/date";
import type { ClinicNavigationIntent } from "../../../../../navigation";
import type { ShiftReadDto, ReservationReadDto } from "../../api/appointmentsApi";
import { useAgendaEvents } from "../hooks/useAgendaEvents";

interface AppointmentsAgendaProps {
  shifts: ShiftReadDto[];
  reservations: ReservationReadDto[];
  onNavigate?: (intent: ClinicNavigationIntent) => void;
}

export function AppointmentsAgenda({ shifts, reservations, onNavigate }: AppointmentsAgendaProps) {
  const [date, setDate] = useState(today(getLocalTimeZone()));
  const [view, setView] = useState<"day" | "week" | "month">("week");

  const events = useAgendaEvents(date, view, shifts, reservations);

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
        onNavigate?.({ kind: "shiftDetail", shiftId });
      } else {
        onNavigate?.({ kind: "reservationDetail", reservationId: id });
      }
    },
  });

  return (
    <Agenda {...agenda}>
      <Agenda.Header>
        <Agenda.Heading />
        <Agenda.ViewSelector />
        <Agenda.Navigation>
          <Agenda.NavButton slot="previous" />
          <Agenda.TodayButton />
          <Agenda.NavButton slot="next" />
        </Agenda.Navigation>
      </Agenda.Header>
      <Agenda.Body>
        <Agenda.WeekHeader />
        <Agenda.AllDaySection>
          <Agenda.AllDayLabel>All Day</Agenda.AllDayLabel>
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
      </Agenda.Body>
    </Agenda>
  );
}
