"use client";

import { ClinicAppointmentSettingsPage } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";

export default function ClinicAppointmentSettingsRoute() {
  return <ClinicAppointmentSettingsPage onNavigate={useClinicNavigation()} />;
}
