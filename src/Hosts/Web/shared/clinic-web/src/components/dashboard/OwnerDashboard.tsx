"use client";

import { NavigationGrid, type NavigationItem } from "../navigation/NavigationGrid";
import type { NavigationIntent } from "../../navigation";
import { useLocalizedText } from "../../localization/index";

interface OwnerDashboardProps {
  onNavigate?: (intent: NavigationIntent) => void;
}

export function OwnerDashboard({ onNavigate }: OwnerDashboardProps) {
  const { t } = useLocalizedText();

  const sections: NavigationItem[] = [
    { title: t("clinic.dashboard.sections.patients.title"), description: t("clinic.dashboard.sections.patients.description"), intent: { kind: "patientIndex" }, icon: "fa-solid fa-users" },
    { title: t("clinic.dashboard.sections.checkIn.title"), description: t("clinic.dashboard.sections.checkIn.description"), intent: { kind: "checkIn" }, icon: "fa-solid fa-clipboard-check" },
    { title: t("clinic.dashboard.sections.appointments.title"), description: t("clinic.dashboard.sections.appointments.description"), intent: { kind: "ownerReservationIndex" }, icon: "fa-solid fa-calendar" },
    { title: t("clinic.dashboard.sections.reminders.title"), description: t("clinic.dashboard.sections.reminders.description"), intent: { kind: "reminders" }, icon: "fa-solid fa-bell" },
    { title: t("clinic.dashboard.sections.settings.title"), description: t("clinic.dashboard.sections.settings.description"), intent: { kind: "clinicSettings" }, icon: "fa-solid fa-gear" },
    { title: t("clinic.dashboard.sections.statistics.title"), description: t("clinic.dashboard.sections.statistics.description"), intent: { kind: "clinicStatistics" }, icon: "fa-solid fa-chart-bar" },
  ];

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">{t("clinic.dashboard.title")}</h1>
        <p className="text-sm text-muted">{t("clinic.dashboard.description")}</p>
      </div>

      <NavigationGrid items={sections} onNavigate={onNavigate} />
    </div>
  );
}
