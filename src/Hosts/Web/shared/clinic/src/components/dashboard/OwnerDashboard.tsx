"use client";

import {
  NavigationGrid,
  type NavigationItem,
} from "../navigation/NavigationGrid";
import type { NavigationIntent } from "../../navigation";
import { useLocalizedText } from "../../localization/index";
import { PageHeader } from "@vitarish/ui";

interface OwnerDashboardProps {
  onNavigate?: (intent: NavigationIntent) => void;
}

export function OwnerDashboard({ onNavigate }: OwnerDashboardProps) {
  const { t } = useLocalizedText();

  const sections: NavigationItem[] = [
    {
      title: t("clinic.dashboard.sections.patients.title"),
      description: t("clinic.dashboard.sections.patients.description"),
      intent: { kind: "patientIndex" },
      icon: "users",
    },
    {
      title: t("clinic.dashboard.sections.checkIn.title"),
      description: t("clinic.dashboard.sections.checkIn.description"),
      intent: { kind: "checkIn" },
      icon: "clipboard-check",
    },
    {
      title: t("clinic.dashboard.sections.appointments.title"),
      description: t("clinic.dashboard.sections.appointments.description"),
      intent: { kind: "ownerReservationIndex" },
      icon: "calendar",
    },
    {
      title: t("clinic.dashboard.sections.reminders.title"),
      description: t("clinic.dashboard.sections.reminders.description"),
      intent: { kind: "reminders" },
      icon: "bell",
    },
    {
      title: t("clinic.dashboard.sections.settings.title"),
      description: t("clinic.dashboard.sections.settings.description"),
      intent: { kind: "clinicSettings" },
      icon: "gear",
    },
    {
      title: t("clinic.dashboard.sections.statistics.title"),
      description: t("clinic.dashboard.sections.statistics.description"),
      intent: { kind: "clinicStatistics" },
      icon: "chart-bar",
    },
  ];

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        icon="gauge"
        title={t("clinic.dashboard.title")}
        details={t("clinic.dashboard.description")}
      />

      <NavigationGrid items={sections} onNavigate={onNavigate} />
    </div>
  );
}
