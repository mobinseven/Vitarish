"use client";

import { useState } from "react";
import { Segment } from "@heroui-pro/react";
import { AnchorPageHeader, FaIcon } from "@vitarish/ui";
import type { NavigationIntent } from "../../../navigation";
import { useLocalizedText } from "../../../localization";
import { OwnerReservationsAgenda } from "../agenda/OwnerReservationsAgenda";
import { VisitBrowser } from "../visits/VisitBrowser";

export interface OwnerReservationListPageProps {
  onNavigate: (intent: NavigationIntent) => void;
}

type ReservationView = "list" | "calendar";

/** Reservations as a list of visits to browse, or as the shift-booking calendar. */
export function OwnerReservationListPage({
  onNavigate,
}: OwnerReservationListPageProps) {
  const { t } = useLocalizedText();
  const [view, setView] = useState<ReservationView>("list");

  return (
    <div className="flex flex-col gap-6">
      <AnchorPageHeader
        icon="calendar-check"
        title={t("clinic.appointments.reservations.title")}
        details={t("clinic.appointments.reservations.description")}
        actions={
          <Segment
            aria-label={t("clinic.appointments.visits.viewLabel")}
            className="bg-surface p-1"
            selectedKey={view}
            variant="ghost"
            onSelectionChange={(key) => setView(key as ReservationView)}
          >
            <Segment.Item id="list">
              <FaIcon aria-hidden="true" icon="list" />
              {t("clinic.appointments.visits.listView")}
            </Segment.Item>
            <Segment.Item id="calendar">
              <FaIcon aria-hidden="true" icon="calendar-week" />
              {t("clinic.appointments.visits.calendarView")}
            </Segment.Item>
          </Segment>
        }
      />
      {view === "list" ? (
        <VisitBrowser onNavigate={onNavigate} />
      ) : (
        <OwnerReservationsAgenda onNavigate={onNavigate} />
      )}
    </div>
  );
}
