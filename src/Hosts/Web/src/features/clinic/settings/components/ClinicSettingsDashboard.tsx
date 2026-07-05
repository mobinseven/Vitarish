"use client";

import { Routes } from "@/app/routes";
import { ClinicNavigationGrid, type ClinicNavigationItem } from "@/features/clinic/components/ClinicNavigationGrid";
import { useLocalizedText } from "@/localization";

export function ClinicSettingsDashboard() {
  const { t } = useLocalizedText();

  const settingsLinks: ClinicNavigationItem[] = [
    { title: t("clinic.settings.sections.users.title"), description: t("clinic.settings.sections.users.description"), href: Routes.Clinic.Settings.Users, icon: "fa-solid fa-user-gear" },
    { title: t("clinic.settings.sections.appointmentSettings.title"), description: t("clinic.settings.sections.appointmentSettings.description"), href: Routes.Clinic.Appointments.Settings, icon: "fa-solid fa-calendar-check" },
    { title: t("clinic.settings.sections.statistics.title"), description: t("clinic.settings.sections.statistics.description"), href: Routes.Clinic.Settings.Statistics, icon: "fa-solid fa-chart-line" },
  ];

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">{t("clinic.settings.title")}</h1>
        <p className="text-sm text-muted">{t("clinic.settings.description")}</p>
      </div>

      <ClinicNavigationGrid items={settingsLinks} />
    </div>
  );
}
