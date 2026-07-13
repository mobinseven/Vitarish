"use client";

import { ClinicNavigationGrid, type ClinicNavigationItem } from "../../navigation/components/ClinicNavigationGrid";
import type { ClinicNavigationIntent } from "../../../../navigation";
import { useLocalizedText } from "../../../../localization";

interface ClinicOwnerDashboardProps {
  onNavigate?: (intent: ClinicNavigationIntent) => void;
}

export function ClinicOwnerDashboard({ onNavigate }: ClinicOwnerDashboardProps) {
  const { t } = useLocalizedText();

  const sections: ClinicNavigationItem[] = [
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

      <ClinicNavigationGrid items={sections} onNavigate={onNavigate} />
    </div>
  );
}
