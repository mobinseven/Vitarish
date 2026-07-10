"use client";

import { Routes } from "@/app/routes";
import { ClinicNavigationGrid, type ClinicNavigationItem } from "@/features/clinic/components/ClinicNavigationGrid";
import { useLocalizedText } from "@/localization";

export function ClinicOwnerDashboard() {
  const { t } = useLocalizedText();

  const sections: ClinicNavigationItem[] = [
    { title: t("clinic.dashboard.sections.patients.title"), description: t("clinic.dashboard.sections.patients.description"), href: Routes.Clinic.Patients.Index, icon: "fa-solid fa-users" },
    { title: t("clinic.dashboard.sections.checkIn.title"), description: t("clinic.dashboard.sections.checkIn.description"), href: Routes.Clinic.CheckIn, icon: "fa-solid fa-clipboard-check" },
    { title: t("clinic.dashboard.sections.appointments.title"), description: t("clinic.dashboard.sections.appointments.description"), href: Routes.Clinic.Appointments.Reservations.Index, icon: "fa-solid fa-calendar" },
    { title: t("clinic.dashboard.sections.reminders.title"), description: t("clinic.dashboard.sections.reminders.description"), href: Routes.Clinic.Reminders, icon: "fa-solid fa-bell" },
    { title: t("clinic.dashboard.sections.settings.title"), description: t("clinic.dashboard.sections.settings.description"), href: Routes.Clinic.Settings.Index, icon: "fa-solid fa-gear" },
    { title: t("clinic.dashboard.sections.statistics.title"), description: t("clinic.dashboard.sections.statistics.description"), href: Routes.Clinic.Settings.Statistics, icon: "fa-solid fa-chart-bar" },
  ];

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">{t("clinic.dashboard.title")}</h1>
        <p className="text-sm text-muted">{t("clinic.dashboard.description")}</p>
      </div>

      <ClinicNavigationGrid items={sections} />
    </div>
  );
}
