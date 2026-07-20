"use client";

import type { NavigationIntent } from "../../../navigation";
import { AppointmentSettingsDashboard } from "./AppointmentSettingsDashboard";

export interface AppointmentSettingsPageProps {
  onNavigate: (intent: NavigationIntent) => void;
}

export function AppointmentSettingsPage({ onNavigate }: AppointmentSettingsPageProps) {
  return <AppointmentSettingsDashboard onNavigate={onNavigate} />;
}
