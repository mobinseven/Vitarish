"use client";

import { useMemo, useState } from "react";
import {
  createCalendar,
  getLocalTimeZone,
  today as todayIn,
  toCalendar,
  type CalendarDate,
} from "@internationalized/date";
import { ClinicSetupHome, useClinicGetSetupProgress, VisitDateStepper } from "@vitarish/clinic";
import { TenantLogoPrompt } from "@vitarish/journey";
import { PageHeader } from "@vitarish/ui";
import { useClinicNavigation } from "@/components/clinic-navigation";
import { getDateCalendar, useLocalizedText } from "@/localization";
import { SurgeriesPanel } from "./SurgeriesPanel";
import { VisitsPanel } from "./VisitsPanel";

/**
 * A day at a glance: its visits and surgeries side by side, both following the day chosen in the header. While a new
 * clinic's owner still has setup to do, the assistant-guided setup comes first; if setup progress cannot be read,
 * the day view shows exactly as before.
 */
export function HomeDashboard() {
  const { t, locale } = useLocalizedText();
  const onNavigate = useClinicNavigation();
  const today = useMemo(
    () => toCalendar(todayIn(getLocalTimeZone()), createCalendar(getDateCalendar(locale))),
    [locale],
  );
  const [focusedDate, setFocusedDate] = useState<CalendarDate>(today);
  const setup = useClinicGetSetupProgress({ query: { retry: false } });

  return (
    <div className="flex flex-col gap-6">
      {setup.data?.applies ? <ClinicSetupHome progress={setup.data} onNavigate={onNavigate} /> : null}
      <PageHeader
        icon="house"
        title={t("home.dashboard.title")}
        actions={
          <div className="rounded-2xl bg-surface p-1">
            <VisitDateStepper focusedDate={focusedDate} period="day" today={today} onFocusedDateChange={setFocusedDate} />
          </div>
        }
      />
      <TenantLogoPrompt />
      <div className="grid gap-6 xl:grid-cols-2">
        <VisitsPanel focusedDate={focusedDate} onNavigate={onNavigate} />
        <SurgeriesPanel focusedDate={focusedDate} onNavigate={onNavigate} />
      </div>
    </div>
  );
}
