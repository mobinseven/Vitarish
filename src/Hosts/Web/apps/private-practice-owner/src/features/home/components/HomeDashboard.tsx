"use client";

import { useMemo, useState } from "react";
import {
  createCalendar,
  getLocalTimeZone,
  today as todayIn,
  toCalendar,
  type CalendarDate,
} from "@internationalized/date";
import { VisitDateStepper } from "@vitarish/clinic";
import { AnchorPageHeader } from "@vitarish/ui";
import { useClinicNavigation } from "@/components/clinic-navigation";
import { getDateCalendar, useLocalizedText } from "@/localization";
import { SurgeriesPanel } from "./SurgeriesPanel";
import { VisitsPanel } from "./VisitsPanel";

/** A day at a glance: its visits and surgeries side by side, both following the day chosen in the header. */
export function HomeDashboard() {
  const { t, locale } = useLocalizedText();
  const onNavigate = useClinicNavigation();
  const today = useMemo(
    () => toCalendar(todayIn(getLocalTimeZone()), createCalendar(getDateCalendar(locale))),
    [locale],
  );
  const [focusedDate, setFocusedDate] = useState<CalendarDate>(today);

  return (
    <div className="flex flex-col gap-6">
      <AnchorPageHeader
        icon="house"
        title={t("home.dashboard.title")}
        actions={
          <div className="rounded-2xl bg-surface p-1">
            <VisitDateStepper focusedDate={focusedDate} period="day" today={today} onFocusedDateChange={setFocusedDate} />
          </div>
        }
      />
      <div className="grid gap-6 xl:grid-cols-2">
        <VisitsPanel focusedDate={focusedDate} onNavigate={onNavigate} />
        <SurgeriesPanel focusedDate={focusedDate} onNavigate={onNavigate} />
      </div>
    </div>
  );
}
