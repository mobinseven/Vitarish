"use client";

import type { NavigationIntent } from "../../../navigation";
import { ServiceConfigurationWorkspace } from "./ServiceConfigurationWorkspace";

interface AppointmentSettingsDashboardProps {
  onNavigate?: (intent: NavigationIntent) => void;
}

export function AppointmentSettingsDashboard({
  onNavigate,
}: AppointmentSettingsDashboardProps) {
  return <ServiceConfigurationWorkspace onNavigate={onNavigate!} />;
}
