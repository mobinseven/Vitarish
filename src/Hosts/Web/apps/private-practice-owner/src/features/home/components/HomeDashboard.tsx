"use client";

import { Button, Calendar, Card, Tooltip } from "@heroui/react";
import { getLocalTimeZone, today, type CalendarDate } from "@internationalized/date";
import Image from "next/image";
import { useMemo, useState, type ReactNode } from "react";
import { useRouter } from "next/navigation";
import { useClinicListSurgeries, useInsuranceProviderSetup, useOwnerReservations } from "@vitarish/clinic";
import { formatDate, formatDateTime, useLocalizedText } from "@/localization";
import { Routes } from "@/app/routes";

const headerImages = { appointments: "/card-red.webp", surgeries: "/card-blue.webp", insurance: "/card-green.webp" } as const;
type DashboardAccent = "appointments" | "surgeries" | "insurance";
type DashboardItem = { id: string; initials: string; name: string; type: string; date?: string; time: string };
const shortId = (value: string) => value.slice(0, 8);
const initials = (value: string) => value.slice(0, 2).toUpperCase();

export function HomeDashboard() {
  const { t, locale } = useLocalizedText();
  const router = useRouter();
  const [selectedDate, setSelectedDate] = useState<CalendarDate | null>(null);
  const [focusedDate, setFocusedDate] = useState<CalendarDate>(() => today(getLocalTimeZone()));
  const dateRange = useMemo(() => {
    const start = new Date();
    const end = new Date(start);
    end.setDate(end.getDate() + 7);
    return { dateFrom: start.toISOString().slice(0, 10), dateTo: end.toISOString().slice(0, 10) };
  }, []);
  const reservations = useOwnerReservations(dateRange);
  const surgeries = useClinicListSurgeries();
  const { context: insuranceContext, credentials } = useInsuranceProviderSetup();
  const visitItems: DashboardItem[] = (reservations.data ?? []).slice(0, 4).map((reservation) => {
    const label = `${t("home.dashboard.reservation")} ${shortId(reservation.id)}`;
    return { id: reservation.id, initials: initials(label), name: label, type: t("home.dashboard.reservationStatus", { status: String(reservation.status) }), time: formatDateTime(reservation.date, locale, { dateStyle: "medium", timeStyle: "short" }) };
  });
  const surgeryItems: DashboardItem[] = (surgeries.data ?? []).slice(0, 4).map((surgery) => ({
    id: surgery.id, initials: initials(surgery.name), name: surgery.name,
    type: surgery.patientId ? `${t("home.dashboard.patient")} ${shortId(surgery.patientId)}` : t("home.dashboard.patient"),
    date: formatDate(surgery.scheduledAt, locale, { dateStyle: "medium" }), time: formatDate(surgery.scheduledAt, locale, { timeStyle: "short" }),
  }));
  const providerRows = credentials.data ?? [];
  const dataError = reservations.error || surgeries.error || insuranceContext.error || credentials.error;

  return <div className="flex flex-col gap-4 pb-8 pt-2">
    <header className="flex flex-col gap-5 sm:flex-row sm:items-end sm:justify-between"><div>
      <div className="mb-2 flex items-center gap-2 text-sm font-semibold text-accent"><i aria-hidden="true" className="fa-light fa-sparkles" />{t("home.dashboard.eyebrow")}</div>
      <h1 className="text-3xl font-semibold tracking-tight text-foreground sm:text-4xl">{t("home.dashboard.greeting")}<i aria-hidden="true" className="fa-light fa-hand-wave ms-3 text-amber-500" /></h1>
      <p className="mt-2 max-w-2xl text-base text-muted">{t("home.dashboard.dateSummary", { date: formatDate(new Date(), locale, { dateStyle: "long" }) })}</p>
      {dataError ? <p className="mt-2 text-sm text-danger" role="alert">{t("home.dashboard.loadFailed")}</p> : null}
    </div></header>

    <div className="grid grid-cols-1 items-start gap-6 xl:grid-cols-3">
      <DashboardPanel accent="appointments" description={t("home.dashboard.appointmentsDescription")} imageSrc={headerImages.appointments} title={t("home.dashboard.appointments")}>
        <DashboardCalendar accent="appointments" focusedDate={focusedDate} label={t("home.dashboard.appointmentsCalendarLabel")} onChange={setSelectedDate} onFocusChange={setFocusedDate} selectedDate={selectedDate} locale={locale} />
        <UpcomingList accent="appointments" emptyLabel={t("home.dashboard.noAppointments")} isLoading={reservations.isLoading} items={visitItems} title={t("home.dashboard.upcomingVisits")} viewAllLabel={t("home.dashboard.viewAll")} onViewAll={() => router.push(Routes.Clinic.Appointments.Reservations.Index)} />
        <PanelActions primary={t("home.dashboard.viewSchedule")} secondary={t("home.dashboard.addAppointment")} onPrimary={() => router.push(Routes.Clinic.Appointments.Reservations.Index)} onSecondary={() => router.push(Routes.Clinic.Appointments.Reservations.Index)} />
      </DashboardPanel>

      <DashboardPanel accent="surgeries" description={t("home.dashboard.surgeriesDescription")} imageSrc={headerImages.surgeries} title={t("home.dashboard.surgeries")}>
        <DashboardCalendar accent="surgeries" focusedDate={focusedDate} label={t("home.dashboard.surgeriesCalendarLabel")} onChange={setSelectedDate} onFocusChange={setFocusedDate} selectedDate={selectedDate} locale={locale} />
        <UpcomingList accent="surgeries" emptyLabel={t("home.dashboard.noSurgeries")} isLoading={surgeries.isLoading} items={surgeryItems} title={t("home.dashboard.upcomingSurgeries")} viewAllLabel={t("home.dashboard.viewAll")} onViewAll={() => router.push(Routes.Clinic.Patients.Index)} showDates />
        <PanelActions primary={t("home.dashboard.viewCalendar")} secondary={t("home.dashboard.addSurgery")} onPrimary={() => router.push(Routes.Clinic.Patients.Index)} onSecondary={() => router.push(Routes.Clinic.Patients.New)} />
      </DashboardPanel>

      <DashboardPanel accent="insurance" description={t("home.dashboard.insuranceDescription")} imageSrc={headerImages.insurance} title={t("home.dashboard.insurance")}>
        <div className="flex flex-col gap-3 p-5 sm:p-6"><div className="grid grid-cols-[minmax(0,1fr)_auto] gap-3 px-1 text-xs font-medium text-muted"><span>{t("home.dashboard.provider")}</span><span aria-hidden="true" /></div>
          <div className="divide-y divide-separator rounded-2xl border border-separator/70 bg-background/55">
            {providerRows.length === 0 && !credentials.isLoading ? <p className="px-4 py-6 text-sm text-muted">{t("home.dashboard.noProviders")}</p> : null}
            {providerRows.map((provider) => { const providerName = provider.providerKey ?? t("home.dashboard.provider"); const isActive = provider.status === 1; const isPending = provider.status === 0; const actionLabel = isActive ? t("home.dashboard.addPrescription") : isPending ? t("home.dashboard.setupProvider") : t("home.dashboard.reconnectProvider"); const actionDisabled = provider.status === 5; return <div className="grid grid-cols-[minmax(0,1fr)_auto] items-center gap-3 px-3 py-4 sm:px-4" key={provider.credentialId ?? providerName}>
              <div className="flex min-w-0 items-center gap-3"><span className="grid size-9 shrink-0 place-items-center rounded-xl bg-accent-soft/60 text-xs font-bold text-accent-soft-foreground">{initials(providerName)}</span><span className="truncate text-sm font-semibold text-foreground">{providerName}</span></div>
              <Tooltip><Button aria-label={`${actionLabel} - ${providerName}`} isDisabled={actionDisabled} size="sm" variant="outline" onPress={() => router.push(isActive ? Routes.Clinic.Settings.PrescriptionTemplates : Routes.Clinic.Settings.Insurance)}><i aria-hidden="true" className={isActive ? "fa-light fa-file-prescription" : "fa-light fa-rotate-right"} /><span className="hidden lg:inline">{actionLabel}</span></Button><Tooltip.Content>{actionLabel}</Tooltip.Content></Tooltip>
            </div>; })}
          </div>
        </div>
        <Tooltip><Button className="mx-5 mb-5 justify-between sm:mx-6 sm:mb-6" fullWidth variant="secondary" onPress={() => router.push(Routes.Clinic.Settings.PrescriptionTemplates)}><span className="flex items-center gap-2"><i aria-hidden="true" className="fa-light fa-grid-2" />{t("home.dashboard.prescriptionsGrid")}</span></Button><Tooltip.Content>{t("home.dashboard.prescriptionsGrid")}</Tooltip.Content></Tooltip>
      </DashboardPanel>
    </div>
  </div>;
}

function DashboardPanel({ accent, children, description, imageSrc, title }: { accent: DashboardAccent; children: ReactNode; description: string; imageSrc: string; title: string }) {
  return <Card className={`dashboard-panel dashboard-panel-${accent} overflow-hidden`}><div className="relative min-h-36 overflow-hidden px-6 py-5 text-white sm:min-h-40 sm:py-6"><Image alt="" className="object-cover" fill sizes="(min-width: 1280px) 33vw, 100vw" src={imageSrc} /><div className="absolute inset-0 bg-gradient-to-br from-black/35 via-black/10 to-black/55" /><div className="relative z-10 flex h-full min-h-24 flex-col justify-between"><span className="grid size-12 place-items-center rounded-2xl bg-white/90 text-xl shadow-lg backdrop-blur-sm"><i aria-hidden="true" className={accent === "appointments" ? "fa-light fa-calendar-days text-rose-500" : accent === "surgeries" ? "fa-light fa-scissors text-blue-500" : "fa-light fa-shield-check text-lime-600"} /></span><div><h2 className="text-2xl font-semibold tracking-tight">{title}</h2><p className="mt-1 text-sm text-white/90">{description}</p></div></div></div>{children}</Card>;
}

function DashboardCalendar({ accent, focusedDate, label, onChange, onFocusChange, selectedDate, locale }: { accent: DashboardAccent; focusedDate: CalendarDate; label: string; onChange: (date: CalendarDate | null) => void; onFocusChange: (date: CalendarDate) => void; selectedDate: CalendarDate | null; locale: Parameters<typeof formatDate>[1] }) {
  return <div className="border-b border-separator/70 p-4 sm:p-5"><Calendar aria-label={label} className={`dashboard-calendar dashboard-calendar-${accent} w-full`} focusedValue={focusedDate} value={selectedDate} onChange={onChange} onFocusChange={onFocusChange} weeksInMonth={6}><Calendar.Header><Calendar.Heading /><div className="flex items-center gap-1"><Calendar.NavButton slot="previous" /><Calendar.NavButton slot="next" /></div></Calendar.Header><Calendar.Grid><Calendar.GridHeader>{(day) => <Calendar.HeaderCell>{day}</Calendar.HeaderCell>}</Calendar.GridHeader><Calendar.GridBody>{(date) => <Calendar.Cell date={date} />}</Calendar.GridBody></Calendar.Grid></Calendar><p className="mt-3 text-xs text-muted">{selectedDate ? `${label}: ${formatDate(selectedDate.toDate(getLocalTimeZone()), locale, { dateStyle: "medium" })}` : label}</p></div>;
}

function UpcomingList({ accent, emptyLabel, isLoading, items, showDates = false, title, viewAllLabel, onViewAll }: { accent: DashboardAccent; emptyLabel: string; isLoading: boolean; items: DashboardItem[]; showDates?: boolean; title: string; viewAllLabel: string; onViewAll: () => void }) {
  return <div className="p-4 sm:p-5"><div className="mb-3 flex items-center justify-between gap-3"><h3 className={`text-sm font-semibold dashboard-accent-text-${accent}`}>{title}</h3><Tooltip><Button className="h-auto min-w-0 p-0 text-xs" variant="ghost" onPress={onViewAll}>{viewAllLabel}</Button><Tooltip.Content>{viewAllLabel}</Tooltip.Content></Tooltip></div>{isLoading ? <p className="py-4 text-sm text-muted" aria-live="polite">{emptyLabel}…</p> : items.length === 0 ? <p className="py-4 text-sm text-muted">{emptyLabel}</p> : <div className="divide-y divide-separator/70">{items.map((item) => <div className="flex items-center gap-3 py-3 first:pt-1 last:pb-1" key={item.id}><span className={`grid size-9 shrink-0 place-items-center rounded-full text-xs font-semibold dashboard-avatar-${accent}`}>{item.initials}</span><div className="min-w-0 flex-1"><p className="truncate text-sm font-semibold text-foreground">{item.name}</p><p className="truncate text-xs text-muted">{item.type}</p></div><div className={`shrink-0 text-end text-xs font-semibold dashboard-accent-text-${accent}`}>{showDates && item.date ? <div>{item.date}</div> : null}<div>{item.time}</div></div></div>)}</div>}</div>;
}

function PanelActions({ primary, secondary, onPrimary, onSecondary }: { primary: string; secondary: string; onPrimary: () => void; onSecondary: () => void }) {
  return <div className="flex flex-col gap-3 border-t border-separator/70 p-4 sm:flex-row sm:p-4"><Tooltip><Button className="min-w-0 flex-1 gap-1 px-2" variant="outline" onPress={onPrimary}><i aria-hidden="true" className="fa-light fa-calendar-days" />{primary}</Button><Tooltip.Content>{primary}</Tooltip.Content></Tooltip><Tooltip><Button className="min-w-0 flex-1 gap-1 px-2" variant="primary" onPress={onSecondary}><i aria-hidden="true" className="fa-light fa-plus" />{secondary}</Button><Tooltip.Content>{secondary}</Tooltip.Content></Tooltip></div>;
}
