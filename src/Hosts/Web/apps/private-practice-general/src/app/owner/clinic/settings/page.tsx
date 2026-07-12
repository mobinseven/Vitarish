"use client";

import { ClinicSettingsDashboard } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";

export default function ClinicSettingsPage() {
  const navigate = useClinicNavigation();
  return <ClinicSettingsDashboard onNavigate={navigate} />;
}
