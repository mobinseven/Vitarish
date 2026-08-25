"use client";

import { SettingsPage } from "@vitarish/clinic";
import { useClinicNavigation } from "@/components/clinic-navigation";

export default function SettingsRoute() {
  return <SettingsPage onNavigate={useClinicNavigation()} />;
}
