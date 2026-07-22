"use client";

import {
  NavigationGrid,
  type NavigationItem,
} from "../../navigation/NavigationGrid";
import type { NavigationIntent } from "../../../navigation";
import { useLocalizedText } from "../../../localization/index";

interface AppointmentSettingsDashboardProps {
  onNavigate?: (intent: NavigationIntent) => void;
}

export function AppointmentSettingsDashboard({
  onNavigate,
}: AppointmentSettingsDashboardProps) {
  const { t } = useLocalizedText();

  const settingsLinks: NavigationItem[] = [
    {
      title: t("clinic.serviceTypes.title"),
      description: t("clinic.serviceTypes.description"),
      intent: { kind: "serviceTypeIndex" },
      icon: "tags",
    },
    {
      title: t("clinic.services.title"),
      description: t("clinic.services.description"),
      intent: { kind: "serviceIndex" },
      icon: "concierge-bell",
    },
    {
      title: t("clinic.serviceProviders.title"),
      description: t("clinic.serviceProviders.description"),
      intent: { kind: "serviceProviderIndex" },
      icon: "user-doctor",
    },
  ];

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">
          {t("clinic.appointments.settings.title")}
        </h1>
        <p className="text-sm text-muted">
          {t("clinic.appointments.settings.description")}
        </p>
      </div>

      <NavigationGrid items={settingsLinks} onNavigate={onNavigate} />
    </div>
  );
}
