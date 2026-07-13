"use client";

import type { ClinicNavigationIntent } from "../../navigation";
import { ClinicSettingsDashboard } from "./ClinicSettingsDashboard";

export interface ClinicSettingsPageProps {
  onNavigate: (intent: ClinicNavigationIntent) => void;
}

export function ClinicSettingsPage({ onNavigate }: ClinicSettingsPageProps) {
  return <ClinicSettingsDashboard onNavigate={onNavigate} />;
}
