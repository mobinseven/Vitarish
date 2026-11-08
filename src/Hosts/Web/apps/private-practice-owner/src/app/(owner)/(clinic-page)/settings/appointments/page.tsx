"use client";

import { AppointmentSettingsPage } from "@vitarish/clinic";
import { useClinicNavigation } from "@/components/clinic-navigation";

export default function AppointmentSettingsRoute() {
  return <AppointmentSettingsPage onNavigate={useClinicNavigation()} />;
}
