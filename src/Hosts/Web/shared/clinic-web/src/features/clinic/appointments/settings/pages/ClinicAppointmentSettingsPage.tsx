"use client";

import type { ClinicNavigationIntent } from "../../../../../navigation";
import { AppointmentSettingsDashboard } from "../components/AppointmentSettingsDashboard";

export interface ClinicAppointmentSettingsPageProps {
  onNavigate: (intent: ClinicNavigationIntent) => void;
}

export function ClinicAppointmentSettingsPage({ onNavigate }: ClinicAppointmentSettingsPageProps) {
  return <AppointmentSettingsDashboard onNavigate={onNavigate} />;
}
