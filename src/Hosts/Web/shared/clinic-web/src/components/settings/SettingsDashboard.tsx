"use client";

import { NavigationGrid, type NavigationItem } from "../navigation/NavigationGrid";
import type { NavigationIntent } from "../../navigation";
import { useLocalizedText } from "../../localization/index";

interface SettingsDashboardProps {
  onNavigate?: (intent: NavigationIntent) => void;
}

export function SettingsDashboard({ onNavigate }: SettingsDashboardProps) {
  const { t } = useLocalizedText();

  const settingsLinks: NavigationItem[] = [
    { title: t("clinic.settings.sections.users.title"), description: t("clinic.settings.sections.users.description"), intent: { kind: "clinicSettingsUsers" }, icon: "fa-solid fa-user-gear" },
    { title: t("clinic.settings.sections.appointmentSettings.title"), description: t("clinic.settings.sections.appointmentSettings.description"), intent: { kind: "appointmentSettings" }, icon: "fa-solid fa-calendar-check" },
    { title: t("clinic.settings.sections.statistics.title"), description: t("clinic.settings.sections.statistics.description"), intent: { kind: "clinicStatistics" }, icon: "fa-solid fa-chart-line" },
  ];

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">{t("clinic.settings.title")}</h1>
        <p className="text-sm text-muted">{t("clinic.settings.description")}</p>
      </div>

      <NavigationGrid items={settingsLinks} onNavigate={onNavigate} />
    </div>
  );
}
