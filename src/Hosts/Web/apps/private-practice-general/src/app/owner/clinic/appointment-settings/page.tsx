"use client";

import { AppointmentSettingsDashboard } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";

export default function ClinicAppointmentSettingsPage() {
  const navigate = useClinicNavigation();
  return <AppointmentSettingsDashboard onNavigate={navigate} />;
}
