"use client";

import { SettingsPage } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";

export default function SettingsRoute() {
  return <SettingsPage onNavigate={useClinicNavigation()} />;
}
