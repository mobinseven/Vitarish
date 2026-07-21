"use client";

import { AppointmentSettingsPage } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";

export default function AppointmentSettingsRoute() {
  return <AppointmentSettingsPage onNavigate={useClinicNavigation()} />;
}
