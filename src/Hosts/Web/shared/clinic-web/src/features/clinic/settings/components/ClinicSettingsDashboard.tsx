"use client";

import { ClinicNavigationGrid, type ClinicNavigationItem } from "../../navigation/components/ClinicNavigationGrid";
import type { ClinicNavigationIntent } from "../../../../navigation";
import { useLocalizedText } from "../../../../localization";

interface ClinicSettingsDashboardProps {
  onNavigate?: (intent: ClinicNavigationIntent) => void;
}

export function ClinicSettingsDashboard({ onNavigate }: ClinicSettingsDashboardProps) {
  const { t } = useLocalizedText();

  const settingsLinks: ClinicNavigationItem[] = [
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

      <ClinicNavigationGrid items={settingsLinks} onNavigate={onNavigate} />
    </div>
  );
}
