"use client";

import type { NavigationIntent } from "../../navigation";
import { SettingsDashboard } from "./SettingsDashboard";

export interface SettingsPageProps {
  onNavigate: (intent: NavigationIntent) => void;
}

export function SettingsPage({ onNavigate }: SettingsPageProps) {
  return <SettingsDashboard onNavigate={onNavigate} />;
}
